// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateDocumentService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateDocumentService : ICandidateDocumentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateDocumentService> _logger;
    private readonly IConfiguration _configuration;

    private const  long MaxDocFileSizeBytes = 10 * 1024 * 1024;  // 10 MB
    private const  long MaxImageSizeBytes   = 5  * 1024 * 1024;  // 5 MB
    private static readonly string[] AllowedDocTypes = { "application/pdf", "image/jpeg", "image/png" };
    private static readonly string[] AllowedImgTypes = { "image/jpeg", "image/png", "image/webp" };

    public CandidateDocumentService(
        AppDbContext context,
        ILogger<CandidateDocumentService> logger,
        IConfiguration configuration)
    {
        _context       = context;
        _logger        = logger;
        _configuration = configuration;
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

            // Resume
            var cv = await _context.CandidateCvs
                .Where(c => c.CandidateId == candidateId)
                .OrderByDescending(c => c.GeneratedAt)
                .FirstOrDefaultAsync();

            // Education certs
            var eduList = await _context.CandidateEducations
                .Where(e => e.CandidateId == candidateId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            // Aadhaar
            var aadhaar = await _context.KycVerifications
                .Where(k => k.CandidateId == candidateId)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefaultAsync();

            // Passport
            var passport = await _context.Set<PassportVerification>()
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
                    Aadhaar  = aadhaar  == null ? null : MapAadhaar(aadhaar)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllDocumentsAsync failed for {CandidateId}", candidateId);
            return DocsFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // 2A — RESUME
    // ════════════════════════════════════════════════
    public async Task<UploadResumeResponseDto> UploadResumeAsync(Guid candidateId, IFormFile file)
    {
        try
        {
            var validationError = ValidateFile(file, AllowedDocTypes, MaxDocFileSizeBytes);
            if (validationError != null)
                return new UploadResumeResponseDto { Success = false, Message = validationError };

            var profile = await _context.CandidateProfiles
                .Include(p => p.Cvs)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new UploadResumeResponseDto { Success = false, Message = "Candidate profile not found." };

            // ── Upload to storage ─────────────────────────────────────────
            // Replace with your actual storage upload call:
            //   var url = await _storageService.UploadAsync($"resumes/{candidateId}/...", file);
            var fileName = $"resumes/{candidateId}/resume_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fileUrl  = $"{_configuration["Storage:BaseUrl"]}/{fileName}";
            // ─────────────────────────────────────────────────────────────

            // Remove old CV records (keep one active resume)
            var oldCvs = profile.Cvs.ToList();
            _context.CandidateCvs.RemoveRange(oldCvs);

            var cv = new CandidateCv
            {
                CvId        = Guid.NewGuid(),
                CandidateId = candidateId,
                CvFileUrl   = fileUrl,
                GeneratedAt = DateTime.UtcNow
                // AI parsing fields (AffindaJobId, ParsedName, etc.) will be populated
                // asynchronously by your resume-parsing background job.
            };

            _context.CandidateCvs.Add(cv);

            profile.ProfileCompletionPct = RecalcPct(profile);
            profile.UpdatedAt            = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UploadResumeResponseDto
            {
                Success              = true,
                Message              = "Resume uploaded. AI parsing will complete shortly.",
                CvId                 = cv.CvId,
                CvFileUrl            = fileUrl,
                ProfileCompletionPct = profile.ProfileCompletionPct
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
    // 2B — EDUCATION CERTIFICATE
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

            // ── Upload to storage ─────────────────────────────────────────
            var fileName = $"education/{candidateId}/cert_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fileUrl  = $"{_configuration["Storage:BaseUrl"]}/{fileName}";
            // ─────────────────────────────────────────────────────────────

            var edu = new CandidateEducation
            {
                EducationId = Guid.NewGuid(),
                CandidateId = candidateId,
                EducationLevel = request.EducationLevel,
                InstituteName = request.InstituteName,
                YearDetails = request.MarksPercentage,
                PassoutYear = request.PassoutYear,
                CertificateUrl = fileUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.CandidateEducations.Add(edu);

            // Update completion %
            var profile = await _context.CandidateProfiles
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile != null)
            {
                profile.ProfileCompletionPct = RecalcPct(profile);
                profile.UpdatedAt            = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new UploadEducationCertificateResponseDto
            {
                Success              = true,
                Message              = "Education certificate uploaded.",
                EducationId          = edu.EducationId,
                CertificateUrl       = fileUrl,
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
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEducationCertificatesAsync failed for {CandidateId}", candidateId);
            return DocsFail("Internal server error.");
        }
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
    // 2C — PASSPORT
    // ════════════════════════════════════════════════
    public async Task<UploadPassportResponseDto> UploadPassportAsync(
        Guid candidateId,
        UploadPassportRequestDto request,
        IFormFile frontImage,
        IFormFile? backImage)
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

            // Remove existing passport entry (one active at a time)
            var existing = await _context.Set<PassportVerification>()
                .Where(p => p.CandidateId == candidateId).ToListAsync();
            _context.Set<PassportVerification>().RemoveRange(existing);

            // ── Upload to storage ─────────────────────────────────────────
            var frontUrl = $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/front_{Guid.NewGuid()}{Path.GetExtension(frontImage.FileName)}";
            string? backUrl  = backImage == null ? null
                : $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/back_{Guid.NewGuid()}{Path.GetExtension(backImage.FileName)}";
            // ─────────────────────────────────────────────────────────────

            var pv = new PassportVerification
            {
                VerificationId   = Guid.NewGuid(),
                CandidateId      = candidateId,
                FrontImageUrl    = frontUrl,
                BackImageUrl     = backUrl,
                AdminDecision    = "Pending",
                CreatedAt        = DateTime.UtcNow
            };

            _context.Set<PassportVerification>().Add(pv);
            await _context.SaveChangesAsync();

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            return new UploadPassportResponseDto
            {
                Success              = true,
                Message              = "Passport uploaded. Pending admin review.",
                VerificationId       = pv.VerificationId,
                FrontImageUrl        = frontUrl,
                BackImageUrl         = backUrl,
                AdminDecision        = "Pending",
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
    // 2D — AADHAAR (KYC)
    // ════════════════════════════════════════════════
    public async Task<UploadAadhaarResponseDto> UploadAadhaarAsync(
        Guid candidateId,
        UploadAadhaarRequestDto request,
        IFormFile frontImage,
        IFormFile? backImage)
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

            // Remove existing
            var existing = await _context.KycVerifications
                .Where(k => k.CandidateId == candidateId).ToListAsync();
            _context.KycVerifications.RemoveRange(existing);

            // ── Upload to storage ─────────────────────────────────────────
            var frontUrl = $"{_configuration["Storage:BaseUrl"]}/aadhaar/{candidateId}/front_{Guid.NewGuid()}{Path.GetExtension(frontImage.FileName)}";
            string? backUrl = backImage == null ? null
                : $"{_configuration["Storage:BaseUrl"]}/aadhaar/{candidateId}/back_{Guid.NewGuid()}{Path.GetExtension(backImage.FileName)}";
            // ─────────────────────────────────────────────────────────────

            var kv = new KycVerification
            {
                VerificationId    = Guid.NewGuid(),
                CandidateId       = candidateId,
                IdType            = "Aadhaar",
                IdFrontImageUrl   = frontUrl,
                IdBackImageUrl    = backUrl,
                IdHash            = string.Empty,  // populate with SHA-256 of file bytes
                AdminDecision     = "Pending",
                CreatedAt         = DateTime.UtcNow
            };

            _context.KycVerifications.Add(kv);
            await _context.SaveChangesAsync();

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            return new UploadAadhaarResponseDto
            {
                Success              = true,
                Message              = "Aadhaar uploaded. Pending admin KYC review.",
                VerificationId       = kv.VerificationId,
                FrontImageUrl        = frontUrl,
                BackImageUrl         = backUrl,
                AdminDecision        = "Pending",
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

    // ════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════

    private static string? ValidateFile(IFormFile? file, string[] allowedTypes, long maxBytes)
    {
        if (file == null || file.Length == 0)
            return "No file provided.";

        if (file.Length > maxBytes)
            return $"File size must not exceed {maxBytes / 1024 / 1024} MB.";

        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return $"Unsupported file type. Allowed: {string.Join(", ", allowedTypes)}.";

        return null;
    }

    private static byte RecalcPct(CandidateProfile p)
    {
        int score = 0;
        if (!string.IsNullOrWhiteSpace(p.FullName))          score += 10;
        if (!string.IsNullOrWhiteSpace(p.ProfilePhotoUrl))   score += 15;
        if (p.DateOfBirth.HasValue)                          score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentCity))       score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentState))      score += 5;
        if (p.TotalExperienceYears > 0)                      score += 10;
        if (p.Cvs?.Any(c => c.CvFileUrl != null) == true)   score += 20;
        if (p.Educations?.Any() == true)                     score += 10;
        if (p.WorkHistories?.Any() == true)                  score += 10;
        if (p.Skills?.Any() == true)                         score += 10;
        return (byte)Math.Min(score, 100);
    }

    private static ResumeDocumentDto MapCv(CandidateCv cv) => new()
    {
        CvId               = cv.CvId,
        CvFileUrl          = cv.CvFileUrl,
        ParsedName         = cv.ParsedName,
        ParsedPhone        = cv.ParsedPhone,
        ParsedEmail        = cv.ParsedEmail,
        ParsedTrade        = cv.ParsedTrade,
        ParsedExperienceYrs = cv.ParsedExperienceYrs,
        ParsedSkills       = cv.ParsedSkills,
        AiConfidenceScore  = cv.AiConfidenceScore,
        UploadedAt         = cv.GeneratedAt,
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
        VerificationStatus = "Pending",
        CreatedAt = e.CreatedAt
    };

    private static PassportDocumentDto MapPassport(PassportVerification p) => new()
    {
        VerificationId  = p.VerificationId,
        FrontImageUrl   = p.FrontImageUrl,
        BackImageUrl    = p.BackImageUrl,
        AiExtractedName = p.AiExtractedName,
        AiExtractedDob  = p.AiExtractedDob,
        AdminDecision   = p.AdminDecision,
        RejectionReason = p.RejectionReason,
        UploadedAt      = p.CreatedAt
    };

    private static AadhaarDocumentDto MapAadhaar(KycVerification k) => new()
    {
        VerificationId     = k.VerificationId,
        FrontImageUrl      = k.IdFrontImageUrl,
        BackImageUrl       = k.IdBackImageUrl,
        AiExtractedName    = k.AiExtractedName,
        AiExtractedDob     = k.AiExtractedDob,
        AiExtractedAddress = k.AiExtractedAddress,
        OcrConfidence      = k.OcrConfidence,
        AdminDecision      = k.AdminDecision,
        RejectionReason    = k.RejectionReason,
        UploadedAt         = k.CreatedAt
    };

    private static CandidateDocumentsResponseDto DocsFail(string msg)
        => new() { Success = false, Message = msg };
}
