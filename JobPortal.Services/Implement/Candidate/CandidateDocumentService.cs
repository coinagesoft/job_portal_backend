// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateDocumentService.cs   (UPDATED — Affinda integrated)
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateDocumentService : ICandidateDocumentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateDocumentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAffindaService _affinda;

    private const long MaxDocFileSizeBytes = 10 * 1024 * 1024;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedDocTypes = { "application/pdf", "image/jpeg", "image/png" };
    private static readonly string[] AllowedImgTypes = { "image/jpeg", "image/png", "image/webp" };

    public CandidateDocumentService(
        AppDbContext context,
        ILogger<CandidateDocumentService> logger,
        IConfiguration configuration,
        IAffindaService affinda)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _affinda = affinda;
    }

    // ════════════════════════════════════════════════
    // GET ALL DOCUMENTS
    // ════════════════════════════════════════════════
    public async Task<CandidateDocumentsResponseDto> GetAllDocumentsAsync(Guid candidateId)
    {
        try
        {
            var profileExists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);

            if (!profileExists)
                return DocsFail("Candidate profile not found.");

            var cv = await _context.CandidateCvs
                .Where(c => c.CandidateId == candidateId)
                .OrderByDescending(c => c.GeneratedAt)
                .FirstOrDefaultAsync();

            var eduList = await _context.CandidateEducations
                .Where(e => e.CandidateId == candidateId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            var aadhaar = await _context.KycVerifications
                .Where(k => k.CandidateId == candidateId)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefaultAsync();

            var passport = await _context.PassportVerifications
                .Where(p => p.CandidateId == candidateId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            return new CandidateDocumentsResponseDto
            {
                Success = true,
                Message = "Documents retrieved.",
                Data = new CandidateDocumentsData
                {
                    Resume = cv == null ? null : MapCv(cv),
                    EducationCertificates = eduList.Select(MapEducation).ToList(),
                    Passport = passport == null ? null : MapPassport(passport),
                    Aadhaar = aadhaar == null ? null : MapAadhaar(aadhaar)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllDocumentsAsync failed for {CandidateId}", candidateId);
            return DocsFail(ex.Message);
        }
    }

    // ════════════════════════════════════════════════
    // UPLOAD RESUME  (Affinda integration)
    // ════════════════════════════════════════════════
    public async Task<UploadResumeResponseDto> UploadResumeAsync(Guid candidateId, IFormFile file)
    {
        try
        {
            // 1. Validate file
            var validationError = ValidateFile(file, AllowedDocTypes, MaxDocFileSizeBytes);
            if (validationError != null)
                return new UploadResumeResponseDto { Success = false, Message = validationError };

            // 2. Load profile with all related data
            var profile = await _context.CandidateProfiles
                .Include(p => p.Cvs)
                .Include(p => p.Skills)
                .Include(p => p.WorkHistories)
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new UploadResumeResponseDto { Success = false, Message = "Candidate profile not found." };

            // 3. Save file to storage (replace with your S3/Azure Blob call)
            var fileName = $"resumes/{candidateId}/resume_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fileUrl = $"{_configuration["Storage:BaseUrl"]}/{fileName}";

            // 4. ── AFFINDA PARSING ──────────────────────────────────────
            _logger.LogInformation("Sending resume to Affinda for candidate {CandidateId}", candidateId);
            var parseResult = await _affinda.ParseResumeAsync(file);

            // 5. Remove old CV (keep only latest)
            _context.CandidateCvs.RemoveRange(profile.Cvs);

            // 6. Create new CandidateCv entity with Affinda data
            var cv = new CandidateCv
            {
                CvId = Guid.NewGuid(),
                CandidateId = candidateId,
                CvFileUrl = fileUrl,
                GeneratedAt = DateTime.UtcNow,

                // Affinda parsed fields
                AffindaJobId = parseResult.AffindaDocId,
                ParsedName = parseResult.ParsedName,
                ParsedPhone = parseResult.ParsedPhone,
                ParsedEmail = parseResult.ParsedEmail,
                ParsedTrade = parseResult.ParsedTrade,
                ParsedExperienceYrs = parseResult.ParsedExperienceYrs,
                ParsedSkills = parseResult.ParsedSkills.Count > 0
                                        ? JsonSerializer.Serialize(parseResult.ParsedSkills)
                                        : null,
                AiConfidenceScore = parseResult.AiConfidenceScore
            };

            _context.CandidateCvs.Add(cv);

            // 7. ── AUTO-FILL PROFILE FIELDS (blank fields only) ─────────
            if (parseResult.Success)
            {
                AutoFillProfileFields(profile, parseResult);

                // 8. ── UPSERT SKILLS ────────────────────────────────────
                await UpsertSkillsAsync(profile, parseResult.ParsedSkills, candidateId);

                // 9. ── UPSERT WORK HISTORIES ────────────────────────────
                await UpsertWorkHistoriesAsync(profile, parseResult.WorkExperiences, candidateId);

                // 10. ── UPSERT EDUCATIONS ───────────────────────────────
                await UpsertEducationsAsync(profile, parseResult.Educations, candidateId);
            }

            // 11. Recalculate profile completion
            profile.ProfileCompletionPct = RecalcPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Resume uploaded & parsed for {CandidateId}. Affinda doc: {DocId}. Skills: {SkillCount}",
                candidateId, parseResult.AffindaDocId, parseResult.ParsedSkills.Count);

            return new UploadResumeResponseDto
            {
                Success = true,
                Message = parseResult.Success
                    ? "Resume uploaded and parsed successfully."
                    : "Resume uploaded. Parsing had issues — some fields may be incomplete.",
                CvId = cv.CvId,
                CvFileUrl = fileUrl,
                ProfileCompletionPct = profile.ProfileCompletionPct,
                AiParsed = parseResult.Success ? new AiParsedResumeDto
                {
                    Name = parseResult.ParsedName,
                    Phone = parseResult.ParsedPhone,
                    Email = parseResult.ParsedEmail,
                    Trade = parseResult.ParsedTrade,
                    ExperienceYrs = parseResult.ParsedExperienceYrs,
                    Skills = parseResult.ParsedSkills,
                    ConfidenceScore = parseResult.AiConfidenceScore,
                    AffindaDocId = parseResult.AffindaDocId
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadResumeAsync failed for {CandidateId}", candidateId);
            return new UploadResumeResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<DeleteResumeResponseDto> DeleteResumeAsync(Guid candidateId)
    {
        try
        {
            var cvs = await _context.CandidateCvs
                .Where(c => c.CandidateId == candidateId)
                .ToListAsync();

            if (!cvs.Any())
                return new DeleteResumeResponseDto { Success = false, Message = "No resume found." };

            _context.CandidateCvs.RemoveRange(cvs);
            await _context.SaveChangesAsync();

            return new DeleteResumeResponseDto { Success = true, Message = "Resume deleted." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteResumeAsync failed for {CandidateId}", candidateId);
            return new DeleteResumeResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    // ════════════════════════════════════════════════
    // EDUCATION CERTIFICATE
    // ════════════════════════════════════════════════
    public async Task<UploadEducationCertificateResponseDto> UploadEducationCertificateAsync(
        Guid candidateId,
        UploadEducationCertificateRequestDto request,
        IFormFile file)
    {
        try
        {
            var validationError = ValidateFile(file, AllowedDocTypes, MaxDocFileSizeBytes);
            if (validationError != null)
                return new UploadEducationCertificateResponseDto { Success = false, Message = validationError };

            var profileExists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!profileExists)
                return new UploadEducationCertificateResponseDto { Success = false, Message = "Candidate not found." };

            var fileUrl = $"{_configuration["Storage:BaseUrl"]}/education/{candidateId}/cert_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var edu = new CandidateEducation
            {
                EducationId = Guid.NewGuid(),
                CandidateId = candidateId,
                EducationLevel = request.EducationLevel,
                InstituteName = request.InstituteName,
                YearDetails = request.MarksPercentage,
                PassoutYear = request.PassoutYear,
                CertificateUrl = fileUrl,
                CertificateNumber = request.CertificateNumber,
                CreatedAt = DateTime.UtcNow
            };
            _context.CandidateEducations.Add(edu);

            var profile = await _context.CandidateProfiles
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile != null)
            {
                profile.ProfileCompletionPct = RecalcPct(profile);
                profile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new UploadEducationCertificateResponseDto
            {
                Success = true,
                Message = "Education certificate uploaded.",
                EducationId = edu.EducationId,
                CertificateUrl = fileUrl,
                ProfileCompletionPct = profile?.ProfileCompletionPct ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadEducationCertificateAsync failed for {CandidateId}", candidateId);
            return new UploadEducationCertificateResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<CandidateDocumentsResponseDto> GetEducationCertificatesAsync(Guid candidateId)
    {
        var eduList = await _context.CandidateEducations
            .Where(e => e.CandidateId == candidateId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        return new CandidateDocumentsResponseDto
        {
            Success = true,
            Message = "Education certificates retrieved.",
            Data = new CandidateDocumentsData
            {
                EducationCertificates = eduList.Select(MapEducation).ToList()
            }
        };
    }

    public async Task<DeleteEducationCertificateResponseDto> DeleteEducationCertificateAsync(
        Guid candidateId, Guid educationId)
    {
        try
        {
            var edu = await _context.CandidateEducations
                .FirstOrDefaultAsync(e => e.EducationId == educationId && e.CandidateId == candidateId);

            if (edu == null)
                return new DeleteEducationCertificateResponseDto
                { Success = false, Message = "Education record not found." };

            _context.CandidateEducations.Remove(edu);
            await _context.SaveChangesAsync();

            return new DeleteEducationCertificateResponseDto { Success = true, Message = "Certificate deleted." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteEducationCertificateAsync failed for {CandidateId}", candidateId);
            return new DeleteEducationCertificateResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    // ════════════════════════════════════════════════
    // PASSPORT
    // ════════════════════════════════════════════════
    public async Task<UploadPassportResponseDto> UploadPassportAsync(
        Guid candidateId, UploadPassportRequestDto request,
        IFormFile frontImage, IFormFile? backImage)
    {
        try
        {
            if (!request.ConsentGiven)
                return new UploadPassportResponseDto
                { Success = false, Message = "Consent is required to upload ID documents." };

            var frontError = ValidateFile(frontImage, AllowedImgTypes, MaxImageSizeBytes);
            if (frontError != null)
                return new UploadPassportResponseDto { Success = false, Message = frontError };

            if (backImage != null)
            {
                var backError = ValidateFile(backImage, AllowedImgTypes, MaxImageSizeBytes);
                if (backError != null)
                    return new UploadPassportResponseDto { Success = false, Message = backError };
            }

            var profileExists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!profileExists)
                return new UploadPassportResponseDto { Success = false, Message = "Candidate not found." };

            var existing = await _context.Set<PassportVerification>()
                .Where(p => p.CandidateId == candidateId).ToListAsync();
            _context.Set<PassportVerification>().RemoveRange(existing);

            var frontUrl = $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/front_{Guid.NewGuid()}{Path.GetExtension(frontImage.FileName)}";
            string? backUrl = backImage == null ? null
                : $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/back_{Guid.NewGuid()}{Path.GetExtension(backImage.FileName)}";

            var pv = new PassportVerification
            {
                VerificationId = Guid.NewGuid(),
                CandidateId = candidateId,
                FrontImageUrl = frontUrl,
                BackImageUrl = backUrl,
                AdminDecision = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<PassportVerification>().Add(pv);
            await _context.SaveChangesAsync();

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            return new UploadPassportResponseDto
            {
                Success = true,
                Message = "Passport uploaded. Pending admin review.",
                VerificationId = pv.VerificationId,
                FrontImageUrl = frontUrl,
                BackImageUrl = backUrl,
                AdminDecision = "Pending",
                ProfileCompletionPct = profile?.ProfileCompletionPct ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadPassportAsync failed for {CandidateId}", candidateId);
            return new UploadPassportResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<DeletePassportResponseDto> DeletePassportAsync(Guid candidateId)
    {
        try
        {
            var records = await _context.Set<PassportVerification>()
                .Where(p => p.CandidateId == candidateId).ToListAsync();

            if (!records.Any())
                return new DeletePassportResponseDto { Success = false, Message = "No passport record found." };

            _context.Set<PassportVerification>().RemoveRange(records);
            await _context.SaveChangesAsync();

            return new DeletePassportResponseDto { Success = true, Message = "Passport document deleted." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeletePassportAsync failed for {CandidateId}", candidateId);
            return new DeletePassportResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    // ════════════════════════════════════════════════
    // AADHAAR
    // ════════════════════════════════════════════════
    public async Task<UploadAadhaarResponseDto> UploadAadhaarAsync(
        Guid candidateId, UploadAadhaarRequestDto request,
        IFormFile frontImage, IFormFile? backImage)
    {
        try
        {
            if (!request.ConsentGiven)
                return new UploadAadhaarResponseDto
                { Success = false, Message = "Consent is required to process Aadhaar data." };

            var frontError = ValidateFile(frontImage, AllowedImgTypes, MaxImageSizeBytes);
            if (frontError != null)
                return new UploadAadhaarResponseDto { Success = false, Message = frontError };

            if (backImage != null)
            {
                var backError = ValidateFile(backImage, AllowedImgTypes, MaxImageSizeBytes);
                if (backError != null)
                    return new UploadAadhaarResponseDto { Success = false, Message = backError };
            }

            var profileExists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!profileExists)
                return new UploadAadhaarResponseDto { Success = false, Message = "Candidate not found." };

            var existing = await _context.KycVerifications
                .Where(k => k.CandidateId == candidateId).ToListAsync();
            _context.KycVerifications.RemoveRange(existing);

            var frontUrl = $"{_configuration["Storage:BaseUrl"]}/aadhaar/{candidateId}/front_{Guid.NewGuid()}{Path.GetExtension(frontImage.FileName)}";
            string? backUrl = backImage == null ? null
                : $"{_configuration["Storage:BaseUrl"]}/aadhaar/{candidateId}/back_{Guid.NewGuid()}{Path.GetExtension(backImage.FileName)}";

            var kv = new KycVerification
            {
                VerificationId = Guid.NewGuid(),
                CandidateId = candidateId,
                IdType = "Aadhaar",
                IdFrontImageUrl = frontUrl,
                IdBackImageUrl = backUrl,
                IdHash = string.Empty,
                AdminDecision = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.KycVerifications.Add(kv);
            await _context.SaveChangesAsync();

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            return new UploadAadhaarResponseDto
            {
                Success = true,
                Message = "Aadhaar uploaded. Pending admin KYC review.",
                VerificationId = kv.VerificationId,
                FrontImageUrl = frontUrl,
                BackImageUrl = backUrl,
                AdminDecision = "Pending",
                ProfileCompletionPct = profile?.ProfileCompletionPct ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadAadhaarAsync failed for {CandidateId}", candidateId);
            return new UploadAadhaarResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<DeleteAadhaarResponseDto> DeleteAadhaarAsync(Guid candidateId)
    {
        try
        {
            var records = await _context.KycVerifications
                .Where(k => k.CandidateId == candidateId).ToListAsync();

            if (!records.Any())
                return new DeleteAadhaarResponseDto { Success = false, Message = "No Aadhaar record found." };

            _context.KycVerifications.RemoveRange(records);
            await _context.SaveChangesAsync();

            return new DeleteAadhaarResponseDto { Success = true, Message = "Aadhaar document deleted." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAadhaarAsync failed for {CandidateId}", candidateId);
            return new DeleteAadhaarResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    // ══════════════════════════════════════════════════════════
    // PRIVATE — Profile auto-fill, skills, work, education upserts
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Auto-fills blank profile fields from Affinda. Never overwrites user data.
    /// </summary>
    private static void AutoFillProfileFields(
        CandidateProfile profile,
        JobPortal.Application.DTOs.AI.AffindaParseResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.PrimaryTrade) && !string.IsNullOrWhiteSpace(result.ParsedTrade))
            profile.PrimaryTrade = result.ParsedTrade;

        if (profile.TotalExperienceYears == 0 && result.ParsedExperienceYrs.HasValue)
            profile.TotalExperienceYears = result.ParsedExperienceYrs.Value;

        if (string.IsNullOrWhiteSpace(profile.CurrentCity) && !string.IsNullOrWhiteSpace(result.City))
            profile.CurrentCity = result.City;

        if (string.IsNullOrWhiteSpace(profile.CurrentState) && !string.IsNullOrWhiteSpace(result.State))
            profile.CurrentState = result.State;

        if (string.IsNullOrWhiteSpace(profile.Nationality) && !string.IsNullOrWhiteSpace(result.Country))
            profile.Nationality = result.Country;
    }

    /// <summary>
    /// Removes AI-sourced skills and replaces with fresh Affinda skill list.
    /// Manually added skills (SkillRole == "Manual") are preserved.
    /// </summary>
    private async Task UpsertSkillsAsync(
        CandidateProfile profile,
        List<string> affindaSkills,
        Guid candidateId)
    {
        if (!affindaSkills.Any()) return;

        // Remove existing AI-sourced skills
        var aiSkills = profile.Skills
            .Where(s => s.SkillRole == "AI" || s.SkillRole == "Affinda")
            .ToList();
        _context.CandidateSkills.RemoveRange(aiSkills);

        // Get existing manual skill names to avoid duplicates
        var existingNames = profile.Skills
            .Where(s => s.SkillRole != "AI" && s.SkillRole != "Affinda")
            .Select(s => s.SkillName.ToLower())
            .ToHashSet();

        // Add new Affinda skills (skip if manually added with same name)
        foreach (var skillName in affindaSkills.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (existingNames.Contains(skillName.ToLower())) continue;

            _context.CandidateSkills.Add(new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = skillName,
                SkillType = "Skill",
                SkillRole = "Affinda"   // tag so we know it came from AI
            });
        }
    }

    /// <summary>
    /// Clears AI-sourced work history and inserts fresh Affinda entries.
    /// Manually added entries are not touched.
    /// </summary>
    private async Task UpsertWorkHistoriesAsync(
        CandidateProfile profile,
        List<JobPortal.Application.DTOs.AI.AffindaWorkExp> affindaWork,
        Guid candidateId)
    {
        if (!affindaWork.Any()) return;

        // Remove work histories that were previously AI-parsed
        // (We tag them by JobDescription containing "[Affinda]" prefix)
        var aiEntries = profile.WorkHistories
            .Where(w => w.JobDescription != null && w.JobDescription.StartsWith("[Affinda]"))
            .ToList();
        _context.CandidateWorkHistories.RemoveRange(aiEntries);

        foreach (var exp in affindaWork)
        {
            if (string.IsNullOrWhiteSpace(exp.JobTitle)) continue;

            var startDate = ParseDatePoint(exp.Dates?.Start) ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = exp.Dates?.End?.IsCurrent == true ? (DateOnly?)null
                          : ParseDatePoint(exp.Dates?.End);

            _context.CandidateWorkHistories.Add(new CandidateWorkHistory
            {
                WorkId = Guid.NewGuid(),
                CandidateId = candidateId,
                JobTitle = exp.JobTitle,
                CompanyName = exp.Organization ?? "Unknown Company",
                StartDate = startDate,
                EndDate = endDate,
                IsCurrent = exp.Dates?.End?.IsCurrent ?? false,
                JobDescription = $"[Affinda]{exp.Description}",  // tag prefix for tracking
                WorkLocation = exp.Location?.Formatted,
                IsOffshore = false
            });
        }
    }

    /// <summary>
    /// Clears AI-sourced education entries and inserts Affinda data.
    /// Only fills if the profile has no education records yet (safe merge).
    /// </summary>
    private async Task UpsertEducationsAsync(
        CandidateProfile profile,
        List<JobPortal.Application.DTOs.AI.AffindaEducation> affindaEdu,
        Guid candidateId)
    {
        if (!affindaEdu.Any()) return;

        // Only auto-insert if no education exists yet — avoid wiping manual records
        var hasExistingEdu = await _context.CandidateEducations
            .AnyAsync(e => e.CandidateId == candidateId);

        if (hasExistingEdu) return;

        foreach (var edu in affindaEdu)
        {
            if (string.IsNullOrWhiteSpace(edu.EducationAccreditation)) continue;

            var level = MapEducationLevel(edu.EducationLevel?.Value ?? edu.EducationLevel?.Label);
            var major = edu.EducationMajor?.FirstOrDefault()?.Trim('(', ')');
            var passoutYear = (short?)(edu.EducationDates?.End?.Year);
            var grade = edu.EducationGrade?.EducationGradeScore?.ToString()
                     ?? edu.EducationGrade?.GradeScore?.ToString();
            var gradeUnit = edu.EducationGrade?.GradeUnit?.Label;

            _context.CandidateEducations.Add(new CandidateEducation
            {
                EducationId = Guid.NewGuid(),
                CandidateId = candidateId,
                EducationLevel = level,
                InstituteName = edu.EducationOrganization,
                YearDetails = grade != null && gradeUnit != null ? $"{grade} {gradeUnit}" : grade,
                PassoutYear = passoutYear,
                CertificateUrl = null,
                IsAiVerified = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    // ══════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════

    private static DateOnly? ParseDatePoint(JobPortal.Application.DTOs.AI.AffindaDatePoint? point)
    {
        if (point == null) return null;
        if (point.Year.HasValue)
        {
            var month = point.Month ?? 1;
            var day = point.Day ?? 1;
            try { return new DateOnly(point.Year.Value, month, day); }
            catch { return new DateOnly(point.Year.Value, 1, 1); }
        }
        if (!string.IsNullOrWhiteSpace(point.Date) &&
            DateOnly.TryParse(point.Date, out var parsed))
            return parsed;
        return null;
    }

    private static string MapEducationLevel(string? affindaLevel) => affindaLevel switch
    {
        "Bachelor" => "Graduate",
        "Master" => "Post Graduate",
        "Doctorate" => "Post Graduate",
        "Diploma" => "Diploma",
        "Course/Certificate" => "ITI",
        "High School" => "12th",
        _ => affindaLevel ?? "Other"
    };

    private static string? ValidateFile(IFormFile? file, string[] allowedTypes, long maxBytes)
    {
        if (file == null || file.Length == 0) return "No file provided.";
        if (file.Length > maxBytes) return $"File size must not exceed {maxBytes / 1024 / 1024} MB.";
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return $"Unsupported file type. Allowed: {string.Join(", ", allowedTypes)}.";
        return null;
    }

    private static byte RecalcPct(CandidateProfile p)
    {
        int score = 0;
        if (!string.IsNullOrWhiteSpace(p.FullName)) score += 10;
        if (!string.IsNullOrWhiteSpace(p.ProfilePhotoUrl)) score += 15;
        if (p.DateOfBirth.HasValue) score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentCity)) score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentState)) score += 5;
        if (p.TotalExperienceYears > 0) score += 10;
        if (p.Cvs?.Any(c => c.CvFileUrl != null) == true) score += 20;
        if (p.Educations?.Any() == true) score += 10;
        if (p.WorkHistories?.Any() == true) score += 10;
        if (p.Skills?.Any() == true) score += 10;
        return (byte)Math.Min(score, 100);
    }

    private static ResumeDocumentDto MapCv(CandidateCv cv) => new()
    {
        CvId = cv.CvId,
        CvFileUrl = cv.CvFileUrl,
        ParsedName = cv.ParsedName,
        ParsedPhone = cv.ParsedPhone,
        ParsedEmail = cv.ParsedEmail,
        ParsedTrade = cv.ParsedTrade,
        ParsedExperienceYrs = cv.ParsedExperienceYrs,
        ParsedSkills = cv.ParsedSkills,
        AiConfidenceScore = cv.AiConfidenceScore,
        UploadedAt = cv.GeneratedAt,
        VerificationStatus = "Pending"
    };

    private static EducationCertificateDto MapEducation(CandidateEducation e) => new()
    {
        EducationId = e.EducationId,
        EducationLevel = e.EducationLevel,
        InstituteName = e.InstituteName,
        MarksPercentage = e.YearDetails,
        PassoutYear = e.PassoutYear,
        CertificateUrl = e.CertificateUrl,
        CertificateNumber = e.CertificateNumber,
        VerificationStatus = "Pending",
        CreatedAt = e.CreatedAt
    };

    private static PassportDocumentDto MapPassport(PassportVerification p) => new()
    {
        VerificationId = p.VerificationId,
        FrontImageUrl = p.FrontImageUrl,
        BackImageUrl = p.BackImageUrl,
        AiExtractedName = p.AiExtractedName,
        AiExtractedDob = p.AiExtractedDob,
        AdminDecision = p.AdminDecision,
        RejectionReason = p.RejectionReason,
        UploadedAt = p.CreatedAt
    };

    private static AadhaarDocumentDto MapAadhaar(KycVerification k) => new()
    {
        VerificationId = k.VerificationId,
        FrontImageUrl = k.IdFrontImageUrl,
        BackImageUrl = k.IdBackImageUrl,
        AiExtractedName = k.AiExtractedName,
        AiExtractedDob = k.AiExtractedDob,
        AiExtractedAddress = k.AiExtractedAddress,
        OcrConfidence = k.OcrConfidence,
        AdminDecision = k.AdminDecision,
        RejectionReason = k.RejectionReason,
        UploadedAt = k.CreatedAt
    };

    private static CandidateDocumentsResponseDto DocsFail(string msg)
        => new() { Success = false, Message = msg };
}
