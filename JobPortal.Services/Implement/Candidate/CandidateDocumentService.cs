// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateDocumentService.cs
//  ALL GIT MERGE CONFLICTS RESOLVED
// ============================================================

using CloudinaryDotNet.Actions;
using JobPortal.Application.DTOs.AI;
using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
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
    private readonly IGeminiDocumentParserService _geminiDocumentParserService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICvGenerationService _cvGeneration;
    private const long MaxDocFileSizeBytes = 10 * 1024 * 1024;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedDocTypes = { "application/pdf", "image/jpeg", "image/png", "application/zip" };
    private static readonly string[] AllowedImgTypes = { "image/jpeg", "image/png", "image/webp" };

    public CandidateDocumentService(
    AppDbContext context,
    ILogger<CandidateDocumentService> logger,
    IConfiguration configuration,
    IAffindaService affinda,
    IFileStorageService fileStorage,
    IGeminiDocumentParserService geminiDocumentParserService,
    ICvGenerationService cvGeneration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _affinda = affinda;
        _fileStorage = fileStorage;
        _geminiDocumentParserService = geminiDocumentParserService;
        _cvGeneration = cvGeneration;
    }

    // ════════════════════════════════════════════════
    // GET ALL DOCUMENTS
    // ════════════════════════════════════════════════
    public async Task<CandidateDocumentsResponseDto> GetAllDocumentsAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
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

            // The Portal CV should exist the moment there's any profile data
            // to build it from — there's no "profile must be X% complete"
            // requirement. If nobody has ever triggered a generation for
            // this candidate yet (e.g. their account predates the
            // auto-generate-on-save behaviour), build one now instead of
            // showing "not generated" indefinitely until their next edit.
            string? generatedCvUrl = profile.GeneratedCvFileUrl;
            DateTime? generatedCvUpdatedAt = profile.GeneratedCvUpdatedAt;

            if (string.IsNullOrWhiteSpace(generatedCvUrl))
            {
                var generated = await _cvGeneration.GenerateCvAsync(candidateId);

                if (generated.Success && !string.IsNullOrWhiteSpace(generated.GeneratedCvUrl))
                {
                    generatedCvUrl = generated.GeneratedCvUrl;
                    generatedCvUpdatedAt = generated.GeneratedAt;
                }
            }

            return new CandidateDocumentsResponseDto
            {
                Success = true,
                Message = "Documents retrieved.",
                Data = new CandidateDocumentsData
                {
                    Resume = cv == null
                        ? null
                        : MapCv(cv),

                    EducationCertificates = eduList
                        .Select(MapEducation)
                        .ToList(),

                    Passport = passport == null
                        ? null
                        : MapPassport(passport),

                    Aadhaar = aadhaar == null
                        ? null
                        : MapAadhaar(aadhaar),

                    GeneratedCv = string.IsNullOrWhiteSpace(generatedCvUrl)
                        ? null
                        : new GeneratedCvDto
                        {
                            Url = generatedCvUrl,
                            UpdatedAt = generatedCvUpdatedAt
                        }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetAllDocumentsAsync failed for {CandidateId}",
                candidateId);

            return DocsFail("Unable to retrieve candidate documents.");
        }
    }
    // ════════════════════════════════════════════════
    // UPLOAD RESUME  (Affinda integration)
    // ════════════════════════════════════════════════
    //public async Task<UploadResumeResponseDto> UploadResumeAsync(Guid candidateId, IFormFile file)
    //{
    //    try
    //    {
    //        // 1. Validate
    //        var validationError = ValidateFile(file, AllowedDocTypes, MaxDocFileSizeBytes);
    //        if (validationError != null)
    //            return new UploadResumeResponseDto { Success = false, Message = validationError };

    //        // 2. Load profile with all related data
    //        var profile = await _context.CandidateProfiles
    //            .Include(p => p.Cvs)
    //            .Include(p => p.Skills)
    //            .Include(p => p.WorkHistories)
    //            .Include(p => p.Educations)
    //            .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

    //        if (profile == null)
    //            return new UploadResumeResponseDto { Success = false, Message = "Candidate profile not found." };

    //        // 3. Build file URL (replace with your actual S3/Azure Blob upload)
    //        var fileUrl =
    //   await _fileStorage.SaveFileAsync(
    //       file,
    //       "resumes");



    //        // 4. Call Affinda
    //        _logger.LogInformation("Sending resume to Affinda for candidate {CandidateId}", candidateId);
    //        var parseResult = await _affinda.ParseResumeAsync(file);
    //        _logger.LogInformation(
    //"Affinda Result => Success:{Success}, Error:{Error}, Name:{Name}, Email:{Email}",
    //parseResult.Success,
    //parseResult.ErrorMessage,
    //parseResult.ParsedName,
    //parseResult.ParsedEmail);
    //        // 5. Remove old CV, keep only latest
    //        _context.CandidateCvs.RemoveRange(profile.Cvs);

    //        // 6. Save new CandidateCv with Affinda data
    //        var cv = new CandidateCv
    //        {
    //            CvId = Guid.NewGuid(),
    //            CandidateId = candidateId,
    //            CvFileUrl = fileUrl,
    //            GeneratedAt = DateTime.UtcNow,
    //            AffindaJobId = parseResult.AffindaDocId,
    //            ParsedName = parseResult.ParsedName,
    //            ParsedPhone = parseResult.ParsedPhone,
    //            ParsedEmail = parseResult.ParsedEmail,
    //            ParsedTrade = parseResult.ParsedTrade,
    //            ParsedExperienceYrs = parseResult.ParsedExperienceYrs,
    //            ParsedSkillsJson = parseResult.ParsedSkills.Count > 0
    //                                    ? JsonSerializer.Serialize(parseResult.ParsedSkills)
    //                                    : null,
    //            AiConfidenceScore = parseResult.AiConfidenceScore
    //        };
    //        _context.CandidateCvs.Add(cv);

    //        // 7. Auto-fill + upsert child tables
    //        if (parseResult.Success)
    //        {
    //            AutoFillProfileFields(profile, parseResult);
    //            await UpsertSkillsAsync(profile, parseResult.ParsedSkills, candidateId);
    //            await UpsertWorkHistoriesAsync(profile, parseResult.WorkExperiences, candidateId);
    //            await UpsertEducationsAsync(profile, parseResult.Educations, candidateId);
    //        }

    //        // 8. Recalculate profile completion
    //        profile.ProfileCompletionPct = RecalcPct(profile);
    //        profile.UpdatedAt = DateTime.UtcNow;

    //        await _context.SaveChangesAsync();

    //        _logger.LogInformation(
    //            "Resume uploaded & parsed for {CandidateId}. Affinda doc: {DocId}. Skills: {SkillCount}",
    //            candidateId, parseResult.AffindaDocId, parseResult.ParsedSkills.Count);

    //        return new UploadResumeResponseDto
    //        {
    //            Success = true,
    //            Message = parseResult.Success
    //                ? "Resume uploaded and parsed successfully."
    //                : "Resume uploaded. Parsing had issues — some fields may be incomplete.",
    //            CvId = cv.CvId,
    //            CvFileUrl = fileUrl,
    //            ProfileCompletionPct = profile.ProfileCompletionPct,
    //            AiParsed = parseResult.Success ? new AiParsedResumeDto
    //            {
    //                Name = parseResult.ParsedName,
    //                Phone = parseResult.ParsedPhone,
    //                Email = parseResult.ParsedEmail,
    //                Trade = parseResult.ParsedTrade,
    //                ExperienceYrs = parseResult.ParsedExperienceYrs,
    //                Skills = parseResult.ParsedSkills,
    //                ConfidenceScore = parseResult.AiConfidenceScore,
    //                AffindaDocId = parseResult.AffindaDocId
    //            } : null
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "UploadResumeAsync failed for {CandidateId}", candidateId);
    //        return new UploadResumeResponseDto { Success = false, Message = "Internal server error." };
    //    }
    //}

    public async Task<UploadResumeResponseDto> UploadResumeAsync(
    Guid candidateId,
    IFormFile file)
    {
        FileUploadResult? uploadResult = null;

        try
        {
            // =====================================================
            // 1. Validate File
            // =====================================================
            var validationError = ValidateFile(
                file,
                AllowedDocTypes,
                MaxDocFileSizeBytes);

            if (validationError != null)
            {
                return new UploadResumeResponseDto
                {
                    Success = false,
                    Message = validationError
                };
            }

            // =====================================================
            // 2. Load Candidate Profile
            // =====================================================
            var profile = await _context.CandidateProfiles
                .Include(x => x.Cvs)
                .Include(x => x.Skills)
                .Include(x => x.WorkHistories)
                .Include(x => x.Educations)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new UploadResumeResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 3. Upload Resume to Cloudinary
            // =====================================================
            uploadResult = await _fileStorage.UploadDocumentAsync(
                file,
                "resumes");

            // =====================================================
            // 4. Delete Previous Resume From Cloudinary
            // =====================================================
            var existingCv = profile.Cvs
                .OrderByDescending(x => x.GeneratedAt)
                .FirstOrDefault();


            // =====================================================
            // 5. Parse Resume Using Affinda
            // =====================================================
            _logger.LogInformation(
                "Sending resume to Affinda for candidate {CandidateId}",
                candidateId);

            var parseResult = await _affinda.ParseResumeAsync(file);

            _logger.LogInformation(
                "Affinda Result => Success:{Success}, Error:{Error}, Name:{Name}, Email:{Email}",
                parseResult.Success,
                parseResult.ErrorMessage,
                parseResult.ParsedName,
                parseResult.ParsedEmail);

            // =====================================================
            // 6. Handle Parsing Result
            // =====================================================

            // =====================================================
            // 6. Verify parse result + candidate name match
            //    The resume is accepted ONLY if it parsed successfully
            //    AND the name on the resume matches the candidate's
            //    profile name. Otherwise the just-uploaded Cloudinary
            //    file is deleted and the upload is rejected.
            // =====================================================
            if (!parseResult.Success)
            {
                _logger.LogWarning(
                    "Resume parsing failed for Candidate {CandidateId}. Error: {Error}",
                    candidateId,
                    parseResult.ErrorMessage);

                await SafeDeleteUploadAsync(uploadResult?.PublicId);

                return new UploadResumeResponseDto
                {
                    Success = false,
                    Message = parseResult.ErrorMessage,
                    ParsedName = parseResult.ParsedName,
                    NameMatched = false
                };
            }

            if (string.IsNullOrWhiteSpace(parseResult.ParsedName))
            {
                _logger.LogWarning(
                    "Resume parsed but no name detected for Candidate {CandidateId}.",
                    candidateId);

                await SafeDeleteUploadAsync(uploadResult?.PublicId);

                return new UploadResumeResponseDto
                {
                    Success = false,
                    Message = parseResult.ErrorMessage,
                    NameMatched = false
                };
            }

            if (!NamesMatch(profile.FullName, parseResult.ParsedName))
            {
                _logger.LogWarning(
                    "Resume name mismatch for Candidate {CandidateId}. Profile='{Profile}', Parsed='{Parsed}'",
                    candidateId,
                    profile.FullName,
                    parseResult.ParsedName);

                await SafeDeleteUploadAsync(uploadResult?.PublicId);

                return new UploadResumeResponseDto
                {
                    Success = false,
                    Message =
                        $"The name on this resume (\"{parseResult.ParsedName}\") does not match your profile name " +
                        $"(\"{profile.FullName}\"). Please upload your own resume.",
                    ParsedName = parseResult.ParsedName,
                    NameMatched = false
                };
            }
            // =====================================================
            // 7. Create CandidateCv Snapshot
            // =====================================================

            var cv = new CandidateCv
            {
                CvId = Guid.NewGuid(),

                CandidateId = candidateId,

                // ===========================
                // Uploaded Resume
                // ===========================
                CvFileUrl = uploadResult.Url,
                CvPublicId = uploadResult.PublicId,

                GeneratedAt = DateTime.UtcNow,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow,

                // ===========================
                // Affinda
                // ===========================
                AffindaJobId = parseResult.AffindaDocId,

                AiConfidenceScore = parseResult.AiConfidenceScore,

                // ===========================
                // Parsed Basic Details
                // ===========================
                ParsedName = parseResult.ParsedName,

                ParsedEmail = parseResult.ParsedEmail,

                ParsedPhone = parseResult.ParsedPhone,

                ParsedTrade = parseResult.ParsedTrade,

                ParsedExperienceYrs = parseResult.ParsedExperienceYrs,

                ParsedSummary = parseResult.ProfessionalSummary,

                ParsedCity = parseResult.City,

                ParsedState = parseResult.State,

                ParsedCountry = parseResult.Country,

                // ===========================
                // Parsed JSON
                // ===========================

                ParsedSkillsJson =
                    parseResult.ParsedSkills?.Any() == true
                        ? JsonSerializer.Serialize(parseResult.ParsedSkills)
                        : null,

                ParsedEducationJson =
                    parseResult.Educations?.Any() == true
                        ? JsonSerializer.Serialize(parseResult.Educations)
                        : null,

                ParsedWorkHistoryJson =
                    parseResult.WorkExperiences?.Any() == true
                        ? JsonSerializer.Serialize(parseResult.WorkExperiences)
                        : null,

                ParsedLanguagesJson =
                    parseResult.Languages?.Any() == true
                        ? JsonSerializer.Serialize(parseResult.Languages)
                        : null,

                // Not available from Affinda yet
                ParsedCertificatesJson = null,

                ParsedProjectsJson = null,

                ParsedRawJson = parseResult.RawAffindaJson
            };

            _context.CandidateCvs.Add(cv);

            // =====================================================
            // 8. Auto Fill Candidate Profile
            // Only fill EMPTY fields.
            // Never overwrite user-entered data.
            // =====================================================

            if (parseResult.Success)
            {
                AutoFillProfileFields(profile, parseResult);
            }

            // Always update because resume upload happened
            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 9. Import Resume Data
            // Only if candidate has not already entered data
            // =====================================================

            if (parseResult.Success)
            {
                // Skills
                if (!profile.Skills.Any())
                {
                    await UpsertSkillsAsync(
                        profile,
                        parseResult.ParsedSkills,
                        candidateId);
                }

                // Work History
                if (!profile.WorkHistories.Any())
                {
                    await UpsertWorkHistoriesAsync(
                        profile,
                        parseResult.WorkExperiences,
                        candidateId);
                }

                // Education
                if (!profile.Educations.Any())
                {
                    await UpsertEducationsAsync(
                        parseResult.Educations,
                        candidateId);
                }

                // Languages (previously parsed but never persisted anywhere queryable)
                await UpsertLanguagesAsync(
                    parseResult.Languages,
                    candidateId);
            }
            // =====================================================
            // 10. Recalculate Profile Completion
            // =====================================================
            var completionData = await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct = completionData?.OverallPct ?? 0;
            // =====================================================
            // 11. Remove Previous Resume From Database
            // =====================================================
            if (existingCv != null)
            {
                _context.CandidateCvs.Remove(existingCv);
            }

            // =====================================================
            // 12. Save All Changes
            // =====================================================
            await _context.SaveChangesAsync();

            // =====================================================
            // 13. Delete Previous Resume From Cloudinary
            // Only after successful database save
            // =====================================================
            if (existingCv != null)
            {
                try
                {
                    await _fileStorage.DeleteAsync(existingCv.CvPublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete previous Cloudinary resume for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 13. Logging
            // =====================================================
            _logger.LogInformation(
                "Resume uploaded successfully for Candidate {CandidateId}. AffindaDocId:{AffindaDocId}",
                candidateId,
                parseResult.AffindaDocId);

            // =====================================================
            // 14. Response
            // =====================================================
            return new UploadResumeResponseDto
            {
                Success = true,

                Message = "Resume uploaded and verified successfully.",

                CvId = cv.CvId,

                CvFileUrl = cv.CvFileUrl,

                ProfileCompletionPct = profile.ProfileCompletionPct,

                ParsedName = parseResult.ParsedName,

                NameMatched = true,

                AiParsed = new AiParsedResumeDto
                {
                    Name = parseResult.ParsedName,
                    Phone = parseResult.ParsedPhone,
                    Email = parseResult.ParsedEmail,
                    Trade = parseResult.ParsedTrade,
                    ExperienceYrs = parseResult.ParsedExperienceYrs,
                    Skills = parseResult.ParsedSkills,
                    ConfidenceScore = parseResult.AiConfidenceScore,
                    AffindaDocId = parseResult.AffindaDocId,

                    City = parseResult.City,
                    State = parseResult.State,
                    Country = parseResult.Country,

                    Languages = MapLanguagesForResponse(parseResult.Languages),
                    Education = MapEducationForResponse(parseResult.Educations),
                    WorkExperience = MapWorkExperienceForResponse(parseResult.WorkExperiences)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UploadResumeAsync failed for Candidate {CandidateId}",
                candidateId);

            // Cleanup newly uploaded Cloudinary file if database save failed
            if (uploadResult != null &&
                !string.IsNullOrWhiteSpace(uploadResult.PublicId))
            {
                try
                {
                    await _fileStorage.DeleteAsync(uploadResult.PublicId);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "Failed to clean up uploaded Cloudinary resume for Candidate {CandidateId}",
                        candidateId);
                }
            }

            return new UploadResumeResponseDto
            {
                Success = false,
                Message = "Unable to upload resume. Please try again later."
            };
        }
    }

    // =====================================================
    // Resume verification helpers
    // =====================================================

    /// <summary>Deletes a just-uploaded Cloudinary file, swallowing errors.</summary>
    private async Task SafeDeleteUploadAsync(string? publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        try
        {
            await _fileStorage.DeleteAsync(publicId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete rejected Cloudinary resume {PublicId}",
                publicId);
        }
    }

    /// <summary>
    /// Returns true when the name parsed from the resume is a reasonable
    /// match for the candidate's stored profile name. Order-insensitive and
    /// tolerant of middle names/initials, but requires the core name tokens
    /// to agree so someone can't upload an unrelated person's CV.
    /// </summary>
    private static bool NamesMatch(string? storedName, string? parsedName)
    {
        var a = NormalizeName(storedName);
        var b = NormalizeName(parsedName);

        if (a.Length == 0 || b.Length == 0)
            return false;

        // Fast path: exact normalized match, or one fully contains the other.
        if (a == b || a.Contains(b) || b.Contains(a))
            return true;

        var tokensA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => t.Length >= 2)
                       .Distinct()
                       .ToList();

        var tokensB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => t.Length >= 2)
                       .Distinct()
                       .ToList();

        if (tokensA.Count == 0 || tokensB.Count == 0)
            return false;

        var common = tokensA.Count(t => tokensB.Contains(t));
        var smaller = Math.Min(tokensA.Count, tokensB.Count);

        // Single-token names need that token to match; multi-token names need
        // at least two tokens in common (e.g. first + last) to avoid matching
        // on a shared surname alone.
        var required = smaller >= 2 ? 2 : 1;

        return common >= required;
    }

    /// <summary>Lowercases, strips titles/punctuation, and collapses whitespace.</summary>
    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLowerInvariant();

        // Keep letters and spaces only (drops dots, commas, digits, etc.)
        var cleaned = new string(
            lowered.Select(c => char.IsLetter(c) || char.IsWhiteSpace(c) ? c : ' ')
                   .ToArray());

        var titles = new HashSet<string> { "mr", "mrs", "ms", "miss", "dr", "shri", "smt", "md" };

        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !titles.Contains(t));

        return string.Join(' ', tokens);
    }

    // =====================================================
    // Unified document upload: parse → verify name → store
    // =====================================================
    public async Task<UploadDocumentResponse> UploadAndVerifyDocumentAsync(
        Guid candidateId,
        IFormFile file)
    {
        FileUploadResult? uploadResult = null;
        string? documentType = null;

        try
        {
            var validationError = ValidateFile(file, AllowedDocTypes, MaxDocFileSizeBytes);
            if (validationError != null)
                return new UploadDocumentResponse
                {
                    Success = false,
                    Message = validationError
                };

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
                return new UploadDocumentResponse
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            // 1. OCR parse (Gemini) — the document type is detected here,
            //    not supplied by the client.
            var parsed = await _geminiDocumentParserService.ParseDocumentAsync(file);

            if (!parsed.Success)
                return new UploadDocumentResponse
                {
                    Success = false,
                    Message = parsed.Message ?? "Could not read this document. Please upload a clearer copy."
                };

            // Document type comes straight from the parser.
            documentType = string.IsNullOrWhiteSpace(parsed.DocumentType)
                ? "Document"
                : parsed.DocumentType.Trim();

            // 2. Extract the name from the parsed fields
            var parsedName = ExtractParsedName(parsed.ParsedData);

            if (string.IsNullOrWhiteSpace(parsedName))
                return new UploadDocumentResponse
                {
                    Success = false,
                    Message = "We couldn't detect a name on this document, so we can't verify it belongs to you.",
                    DocumentType = documentType,
                    ParsedData = parsed.ParsedData,
                    NameMatched = false
                };

            // 3. Verify parsed name == candidate profile name
            if (!NamesMatch(profile.FullName, parsedName))
            {
                _logger.LogWarning(
                    "Document name mismatch for Candidate {CandidateId} ({Type}). Profile='{Profile}', Parsed='{Parsed}'",
                    candidateId, documentType, profile.FullName, parsedName);

                return new UploadDocumentResponse
                {
                    Success = false,
                    Message =
                        $"The name on this document (\"{parsedName}\") does not match your profile name " +
                        $"(\"{profile.FullName}\"). Please upload your own document.",
                    DocumentType = documentType,
                    ParsedName = parsedName,
                    NameMatched = false,
                    ParsedData = parsed.ParsedData
                };
            }


            var shortId = candidateId.ToString("N").Substring(0, 8);
            var fileName = $"{Slugify(documentType)}_{Slugify(profile.FullName)}_{shortId}";

            uploadResult = await _fileStorage.UploadDocumentAsync(
                file,
                "candidate-documents",
                fileName);

            // 5. Replace any existing document of the same (detected) type
            var existing = await _context.CandidateDocuments
                .Where(d => d.CandidateId == candidateId && d.DocumentType == documentType)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var doc = new CandidateDocument
            {
                DocumentId = Guid.NewGuid(),
                CandidateId = candidateId,
                DocumentType = documentType,
                FileUrl = uploadResult.Url,
                FilePublicId = uploadResult.PublicId,
                ParsedName = parsedName,
                ParsedDataJson = parsed.ParsedData.HasValue
                    ? parsed.ParsedData.Value.GetRawText()
                    : parsed.RawResponse,
                VerificationStatus = "Verified",
                UploadedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.CandidateDocuments.Add(doc);

            // Save/update KYC verification for Aadhaar
            if (documentType.Equals("Aadhaar Card", StringComparison.OrdinalIgnoreCase))
            {
                var kyc = await _context.KycVerifications
                    .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

                if (kyc == null)
                {
                    kyc = new KycVerification
                    {
                        VerificationId = Guid.NewGuid(),
                        CandidateId = candidateId,
                        CreatedAt = now
                    };

                    _context.KycVerifications.Add(kyc);
                }

                // Basic document details
                kyc.IdType = "Aadhaar";
                kyc.IdFrontImageUrl = uploadResult.Url;
                kyc.IdFrontPublicId = uploadResult.PublicId;

                // AI extracted data
                kyc.AiExtractedName = parsedName;

                kyc.AiExtractedDocumentNumber = GetParsedField(
                    parsed.ParsedData,
                    "aadhaarNumber",
                    "aadhaar_number",
                    "aadhaar");

                kyc.AiExtractedAddress = GetParsedField(
                    parsed.ParsedData,
                    "address");

                kyc.AiExtractedGender = GetParsedField(
                    parsed.ParsedData,
                    "gender");

                kyc.AiExtractedDob = TryParseDateOnly(
                    GetParsedField(
                        parsed.ParsedData,
                        "dob",
                        "dateOfBirth",
                        "date_of_birth"));

                // Gemini returns values like 0.98 -> store as 98
                kyc.AiConfidenceScore = parsed.AiConfidenceScore.HasValue
                    ? parsed.AiConfidenceScore.Value * 100
                    : null;

                // Required for duplicate detection
                if (string.IsNullOrWhiteSpace(kyc.IdHash))
                {
                    var documentNumber = kyc.AiExtractedDocumentNumber;

                    kyc.IdHash = !string.IsNullOrWhiteSpace(documentNumber)
                        ? documentNumber.Replace(" ", "").Replace("-", "")
                        : Guid.NewGuid().ToString("N");
                }

                // Verification
                kyc.AdminDecision = "Verified";
                kyc.IsImportedToProfile = false;

                // Audit
                kyc.UpdatedAt = now;
            }
            if (documentType.Equals("Passport", StringComparison.OrdinalIgnoreCase))
            {
                var passport = await _context.PassportVerifications
                    .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

                if (passport == null)
                {
                    passport = new PassportVerification
                    {
                        VerificationId = Guid.NewGuid(),
                        CandidateId = candidateId,
                        CreatedAt = now
                    };

                    _context.PassportVerifications.Add(passport);
                }

                passport.FrontImageUrl = uploadResult.Url;
                passport.FrontPublicId = uploadResult.PublicId;

                passport.AiExtractedName = parsedName;
                passport.AiConfidenceScore = parsed.AiConfidenceScore;
                passport.AdminDecision = "Verified";
                passport.IsImportedToProfile = false;
                passport.UpdatedAt = now;
            }

            if (existing.Count > 0)
                _context.CandidateDocuments.RemoveRange(existing);

            // 5b. If this is an ITI certificate, also persist/refresh the
            //     ITI certificate review row (iti_certificate_reviews) with the
            //     AI-extracted fields, so it shows up in the review/import flow.
            if (IsItiCertificate(documentType))
            {
                var trade = GetParsedField(parsed.ParsedData, "trade");
                var institute = GetParsedField(parsed.ParsedData,
                    "institute", "college", "iti", "institution");
                var certNo = GetParsedField(parsed.ParsedData,
                    "certificate no", "certificate number", "certno",
                    "registration", "reg no", "roll");
                short? year = TryExtractYear(GetParsedField(parsed.ParsedData,
                    "year", "passing", "session"));

                var existingReview = await _context.ItiCertificateReviews
                    .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

                if (existingReview == null)
                {
                    _context.ItiCertificateReviews.Add(new ItiCertificateReview
                    {
                        ItiReviewId = Guid.NewGuid(),
                        CandidateId = candidateId,
                        ItiCertImageUrl = uploadResult.Url,
                        ItiCertPublicId = uploadResult.PublicId,
                        AiExtractedTrade = trade,
                        AiExtractedInstitute = institute,
                        AiExtractedYear = year,
                        AiExtractedCertNo = certNo,
                        AiConfidenceScore = 95m,
                        IsImportedToProfile = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                else
                {
                    existingReview.ItiCertImageUrl = uploadResult.Url;
                    existingReview.ItiCertPublicId = uploadResult.PublicId;
                    existingReview.AiExtractedTrade = trade;
                    existingReview.AiExtractedInstitute = institute;
                    existingReview.AiExtractedYear = year;
                    existingReview.AiExtractedCertNo = certNo;
                    existingReview.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();

            // Clean up previous Cloudinary files only after a successful save
            foreach (var old in existing)
                await SafeDeleteUploadAsync(old.FilePublicId);

            _logger.LogInformation(
                "Document {Type} stored for Candidate {CandidateId} as '{FileName}'. DocumentId={DocumentId}",
                documentType, candidateId, fileName, doc.DocumentId);

            return new UploadDocumentResponse
            {
                Success = true,
                Message = $"{documentType} uploaded and verified successfully.",
                DocumentId = doc.DocumentId,
                DocumentType = documentType,
                FileUrl = doc.FileUrl,
                ParsedName = parsedName,
                NameMatched = true,
                ParsedData = parsed.ParsedData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UploadAndVerifyDocumentAsync failed for Candidate {CandidateId} ({Type})",
                candidateId, documentType);

            await SafeDeleteUploadAsync(uploadResult?.PublicId);

            return new UploadDocumentResponse
            {
                Success = false,
                Message = ex.ToString(), // TEMPORARY
                DocumentType = documentType
            };
        }
    }
    private static DateOnly? TryParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string[] formats =
        {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy-MM-dd",
        "MM/dd/yyyy",
        "M/d/yyyy"
    };

        if (DateOnly.TryParseExact(
                value.Trim(),
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        // Fallback
        if (DateTime.TryParse(value, out var dt))
            return DateOnly.FromDateTime(dt);

        return null;
    }
    private static bool IsItiCertificate(string? documentType)
        => !string.IsNullOrWhiteSpace(documentType) &&
           documentType.Replace("-", " ").Replace("_", " ")
               .IndexOf("iti", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>Finds the first parsed field whose key contains any of the given substrings.</summary>
    private static string? GetParsedField(System.Text.Json.JsonElement? data, params string[] keyContains)
    {
        if (data is not { ValueKind: System.Text.Json.JsonValueKind.Object } el)
            return null;

        foreach (var prop in el.EnumerateObject())
        {
            var name = prop.Name.Replace("_", " ").Replace("-", " ").ToLowerInvariant();
            foreach (var k in keyContains)
            {
                if (name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var v = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(v))
                        return v.Trim();
                }
            }
        }
        return null;
    }

    /// <summary>Pulls a 4-digit year out of a free-text value (e.g. "Aug 2018").</summary>
    private static short? TryExtractYear(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(s, @"(19|20)\d{2}");
        return m.Success && short.TryParse(m.Value, out var y) ? y : (short?)null;
    }

    /// <summary>Pulls the candidate name out of the parser's dynamic field set.</summary>
    private static string? ExtractParsedName(System.Text.Json.JsonElement? data)
    {
        if (data is null ||
            data.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
            return null;

        foreach (var key in new[] { "name", "Name", "fullName", "FullName", "candidateName" })
        {
            if (data.Value.TryGetProperty(key, out var v) &&
                v.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
        }

        return null;
    }

    /// <summary>Turns a document type into a safe Cloudinary folder segment.</summary>
    private static string MakeSafeDocumentFolder(string documentType)
    {
        var cleaned = new string(
            documentType.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "general" : cleaned.ToLowerInvariant();
    }

    /// <summary>
    /// Lowercases a value and replaces every run of non-alphanumeric characters
    /// with a single underscore, e.g. "Aadhaar Card" -> "aadhaar_card".
    /// </summary>
    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "doc";

        var sb = new System.Text.StringBuilder();
        var lastUnderscore = false;

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastUnderscore = false;
            }
            else if (!lastUnderscore)
            {
                sb.Append('_');
                lastUnderscore = true;
            }
        }

        var slug = sb.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "doc" : slug;
    }

    public async Task<DeleteResumeResponseDto> DeleteResumeAsync(Guid candidateId)
    {
        try
        {
            // =====================================================
            // 1. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .Include(x => x.Cvs)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new DeleteResumeResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 2. Find Latest Resume
            // =====================================================

            var resume = profile.Cvs
                .OrderByDescending(x => x.GeneratedAt)
                .FirstOrDefault();

            if (resume == null)
            {
                return new DeleteResumeResponseDto
                {
                    Success = false,
                    Message = "Resume not found."
                };
            }

            // Store PublicId before removing entity
            var publicId = resume.CvPublicId;

            // =====================================================
            // 3. Remove Resume From Database
            // =====================================================

            _context.CandidateCvs.Remove(resume);

            // =====================================================
            // 4. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 5. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 6. Delete Resume From Cloudinary
            // Only after successful database save
            // =====================================================

            if (!string.IsNullOrWhiteSpace(publicId))
            {
                try
                {
                    await _fileStorage.DeleteAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete resume from Cloudinary for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 7. Logging
            // =====================================================

            _logger.LogInformation(
                "Resume deleted successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 8. Response
            // =====================================================

            return new DeleteResumeResponseDto
            {
                Success = true,
                Message = "Resume deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteResumeAsync failed for Candidate {CandidateId}",
                candidateId);

            return new DeleteResumeResponseDto
            {
                Success = false,
                Message = "Unable to delete resume. Please try again later."
            };
        }
    }

    public async Task<UploadEducationCertificateResponseDto> UploadEducationCertificateAsync(
     Guid candidateId,
     UploadEducationCertificateRequestDto request,
     IFormFile file)
    {
        FileUploadResult? uploadResult = null;

        ItiCertificateReview? existingItiReview = null;

        string? oldCertificatePublicId = null;

        bool isItiCertificate =
            request.EducationLevel.Equals(
                "ITI",
                StringComparison.OrdinalIgnoreCase);

        try
        {
            // =====================================================
            // 1. Validate Certificate
            // =====================================================

            var validationError = ValidateFile(
                file,
                AllowedDocTypes,
                MaxDocFileSizeBytes);

            if (validationError != null)
            {
                return new UploadEducationCertificateResponseDto
                {
                    Success = false,
                    Message = validationError
                };
            }

            // =====================================================
            // 2. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .Include(x => x.Educations)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new UploadEducationCertificateResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 3. Find Existing Education Record
            // =====================================================

            CandidateEducation? existingEducation = null;

            if (request.EducationId.HasValue)
            {
                existingEducation = await _context.CandidateEducations
                    .FirstOrDefaultAsync(x =>
                        x.EducationId == request.EducationId.Value &&
                        x.CandidateId == candidateId);

                if (existingEducation != null)
                {
                    oldCertificatePublicId =
                        existingEducation.CertificatePublicId;
                }
            }

            // =====================================================
            // 4. Upload Certificate to Cloudinary
            // =====================================================

            uploadResult = await _fileStorage.UploadDocumentAsync(
       file,
       "education");

            // =====================================================
            // Load Existing ITI Review
            // =====================================================

            if (isItiCertificate)
            {
                existingItiReview = await _context.ItiCertificateReviews
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == candidateId);
            }

            // =====================================================
            // 5. Create or Update Education
            // =====================================================

            CandidateEducation education;

            if (existingEducation == null)
            {
                education = new CandidateEducation
                {
                    EducationId = Guid.NewGuid(),

                    CandidateId = candidateId,

                    EducationLevel = request.EducationLevel,

                    InstituteName = request.InstituteName,

                    YearDetails = request.MarksPercentage,

                    PassoutYear = request.PassoutYear,

                    CertificateNumber = request.CertificateNumber,

                    CertificateUrl = uploadResult.Url,

                    CertificatePublicId = uploadResult.PublicId,

                    IsAiVerified = false,

                    CreatedAt = DateTime.UtcNow
                };

                _context.CandidateEducations.Add(education);
            }
            else
            {
                education = existingEducation;

                education.EducationLevel = request.EducationLevel;

                education.InstituteName = request.InstituteName;

                education.YearDetails = request.MarksPercentage;

                education.PassoutYear = request.PassoutYear;

                education.CertificateNumber = request.CertificateNumber;

                education.CertificateUrl = uploadResult.Url;

                education.CertificatePublicId = uploadResult.PublicId;
            }

            // =====================================================
            // 6. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 7. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 8. Delete Previous Certificate From Cloudinary
            // Only after successful database save
            // =====================================================

            if (existingEducation != null &&
                !string.IsNullOrWhiteSpace(existingEducation.CertificatePublicId) &&
                existingEducation.CertificatePublicId != uploadResult.PublicId)
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        existingEducation.CertificatePublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete previous education certificate for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 9. Logging
            // =====================================================

            _logger.LogInformation(
                "Education certificate uploaded successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 10. Response
            // =====================================================

            return new UploadEducationCertificateResponseDto
            {
                Success = true,

                Message = existingEducation == null
                    ? "Education certificate uploaded successfully."
                    : "Education certificate updated successfully.",

                EducationId = education.EducationId,

                CertificateUrl = education.CertificateUrl,

                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UploadEducationCertificateAsync failed for Candidate {CandidateId}",
                candidateId);

            return new UploadEducationCertificateResponseDto
            {
                Success = false,
                Message = "Unable to upload education certificate. Please try again later."
            };
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
      Guid candidateId,
      Guid educationId)
    {
        try
        {
            // =====================================================
            // 1. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .Include(x => x.Educations)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new DeleteEducationCertificateResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 2. Find Education Record
            // =====================================================

            var education = profile.Educations
                .FirstOrDefault(x => x.EducationId == educationId);

            if (education == null)
            {
                return new DeleteEducationCertificateResponseDto
                {
                    Success = false,
                    Message = "Education record not found."
                };
            }

            // Store PublicId before removing entity
            var publicId = education.CertificatePublicId;

            // =====================================================
            // 3. Remove Education Record
            // =====================================================

            _context.CandidateEducations.Remove(education);

            // =====================================================
            // 4. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 5. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 6. Delete Certificate From Cloudinary
            // Only after successful database save
            // =====================================================

            if (!string.IsNullOrWhiteSpace(publicId))
            {
                try
                {
                    await _fileStorage.DeleteAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete education certificate from Cloudinary for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 7. Logging
            // =====================================================

            _logger.LogInformation(
                "Education certificate deleted successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 8. Response
            // =====================================================

            return new DeleteEducationCertificateResponseDto
            {
                Success = true,
                Message = "Education certificate deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteEducationCertificateAsync failed for Candidate {CandidateId}",
                candidateId);

            return new DeleteEducationCertificateResponseDto
            {
                Success = false,
                Message = "Unable to delete education certificate. Please try again later."
            };
        }
    }


    // ════════════════════════════════════════════════
    // PASSPORT
    // ════════════════════════════════════════════════
    //public async Task<UploadPassportResponseDto> UploadPassportAsync(
    //    Guid candidateId, UploadPassportRequestDto request,
    //    IFormFile frontImage, IFormFile? backImage)
    //{
    //    try
    //    {
    //        if (!request.ConsentGiven)
    //            return new UploadPassportResponseDto
    //            { Success = false, Message = "Consent is required to upload ID documents." };

    //        var frontError = ValidateFile(frontImage, AllowedImgTypes, MaxImageSizeBytes);
    //        if (frontError != null)
    //            return new UploadPassportResponseDto { Success = false, Message = frontError };

    //        if (backImage != null)
    //        {
    //            var backError = ValidateFile(backImage, AllowedImgTypes, MaxImageSizeBytes);
    //            if (backError != null)
    //                return new UploadPassportResponseDto { Success = false, Message = backError };
    //        }

    //        var profileExists = await _context.CandidateProfiles
    //            .AnyAsync(p => p.CandidateId == candidateId);
    //        if (!profileExists)
    //            return new UploadPassportResponseDto { Success = false, Message = "Candidate not found." };

    //        var existing = await _context.Set<PassportVerification>()
    //            .Where(p => p.CandidateId == candidateId).ToListAsync();
    //        _context.Set<PassportVerification>().RemoveRange(existing);

    //        var frontUrl = $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/front_{Guid.NewGuid()}{Path.GetExtension(frontImage.FileName)}";
    //        string? backUrl = backImage == null ? null
    //            : $"{_configuration["Storage:BaseUrl"]}/passport/{candidateId}/back_{Guid.NewGuid()}{Path.GetExtension(backImage.FileName)}";

    //        var pv = new PassportVerification
    //        {
    //            VerificationId = Guid.NewGuid(),
    //            CandidateId = candidateId,
    //            FrontImageUrl = frontUrl,
    //            BackImageUrl = backUrl,
    //            AdminDecision = "Pending",
    //            CreatedAt = DateTime.UtcNow
    //        };

    //        _context.Set<PassportVerification>().Add(pv);
    //        await _context.SaveChangesAsync();

    //        var profile = await _context.CandidateProfiles
    //            .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

    //        return new UploadPassportResponseDto
    //        {
    //            Success = true,
    //            Message = "Passport uploaded. Pending admin review.",
    //            VerificationId = pv.VerificationId,
    //            FrontImageUrl = frontUrl,
    //            BackImageUrl = backUrl,
    //            AdminDecision = "Pending",
    //            ProfileCompletionPct = profile?.ProfileCompletionPct ?? 0
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "UploadPassportAsync failed for {CandidateId}", candidateId);
    //        return new UploadPassportResponseDto { Success = false, Message = "Internal server error." };
    //    }
    //}


    public async Task<UploadPassportResponseDto> UploadPassportAsync(
    Guid candidateId,
    UploadPassportRequestDto request,
    IFormFile frontImage,
    IFormFile? backImage)
    {
        FileUploadResult? frontUpload = null;
        FileUploadResult? backUpload = null;

        try
        {
            // =====================================================
            // 1. Consent Validation
            // =====================================================

            if (!request.ConsentGiven)
            {
                return new UploadPassportResponseDto
                {
                    Success = false,
                    Message = "Consent is required to upload passport."
                };
            }

            // =====================================================
            // 2. Validate Front Image
            // =====================================================

            var frontError = ValidateFile(
                frontImage,
                AllowedImgTypes,
                MaxImageSizeBytes);

            if (frontError != null)
            {
                return new UploadPassportResponseDto
                {
                    Success = false,
                    Message = frontError
                };
            }

            // =====================================================
            // 3. Validate Back Image
            // =====================================================

            if (backImage != null)
            {
                var backError = ValidateFile(
                    backImage,
                    AllowedImgTypes,
                    MaxImageSizeBytes);

                if (backError != null)
                {
                    return new UploadPassportResponseDto
                    {
                        Success = false,
                        Message = backError
                    };
                }
            }

            // =====================================================
            // 4. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new UploadPassportResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 5. Find Existing Passport
            // =====================================================

            var existingPassport = await _context.PassportVerifications
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId);

            // =====================================================
            // 6. Upload Front Image to Cloudinary
            // =====================================================

            frontUpload = await _fileStorage.UploadDocumentAsync(
                frontImage,
                "passport/front");

            // =====================================================
            // 7. Upload Back Image
            // =====================================================

            if (backImage != null)
            {
                backUpload = await _fileStorage.UploadDocumentAsync(
                    backImage,
                    "passport/back");
            }

            // =====================================================
            // 8. Parse Passport Using Gemini OCR
            // =====================================================

            _logger.LogInformation(
                "Sending Passport to Gemini OCR for Candidate {CandidateId}",
                candidateId);

            var parseResult =
                await _geminiDocumentParserService.ParseDocumentAsync(frontImage);

            if (!parseResult.Success)
            {
                _logger.LogWarning(
                    "Gemini OCR failed for Candidate {CandidateId}. Error: {Error}",
                    candidateId,
                    parseResult.Message);
            }

            // =====================================================
            // 9. Read OCR Fields
            // =====================================================

            string? extractedName = null;
            DateOnly? extractedDob = null;

            if (parseResult.Success &&
                parseResult.ParsedData.HasValue)
            {
                var fields = parseResult.ParsedData.Value;

                if (fields.TryGetProperty("name", out var name))
                    extractedName = name.GetString();

                if (fields.TryGetProperty("dob", out var dob))
                {
                    if (DateOnly.TryParse(dob.GetString(), out var parsedDob))
                        extractedDob = parsedDob;
                }
            }

            // =====================================================
            // 10. Remove Previous Passport Record
            // =====================================================

            if (existingPassport != null)
            {
                _context.PassportVerifications.Remove(existingPassport);
            }

            // =====================================================
            // 11. Create Passport Verification
            // =====================================================

            var verification = new PassportVerification
            {
                VerificationId = Guid.NewGuid(),

                CandidateId = candidateId,

                FrontImageUrl = frontUpload!.Url,
                FrontPublicId = frontUpload.PublicId,

                BackImageUrl = backUpload?.Url,
                BackPublicId = backUpload?.PublicId,

                AiExtractedName = extractedName,

                AiExtractedDob = extractedDob,

                AiConfidenceScore = null,

                AdminDecision = "Pending",

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            _context.PassportVerifications.Add(verification);

            // =====================================================
            // 12. Auto Fill Candidate Profile
            // Only fill EMPTY fields
            // =====================================================

            if (string.IsNullOrWhiteSpace(profile.FullName) &&
                !string.IsNullOrWhiteSpace(extractedName))
            {
                profile.FullName = extractedName;
            }

            if (!profile.DateOfBirth.HasValue &&
                extractedDob.HasValue)
            {
                profile.DateOfBirth = extractedDob.Value;
            }

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 13. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            // =====================================================
            // 14. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 15. Delete Previous Passport Images
            // Only after successful database save
            // =====================================================

            if (existingPassport != null)
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        existingPassport.FrontPublicId);

                    await _fileStorage.DeleteAsync(
                        existingPassport.BackPublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete previous Passport images for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 16. Logging
            // =====================================================

            _logger.LogInformation(
                "Passport uploaded successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 17. Response
            // =====================================================

            return new UploadPassportResponseDto
            {
                Success = true,

                Message = parseResult.Success
                    ? "Passport uploaded and processed successfully."
                    : "Passport uploaded successfully. OCR could not extract all information.",

                VerificationId = verification.VerificationId,

                FrontImageUrl = verification.FrontImageUrl,

                BackImageUrl = verification.BackImageUrl,

                AdminDecision = verification.AdminDecision,

                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UploadPassportAsync failed for Candidate {CandidateId}",
                candidateId);

            return new UploadPassportResponseDto
            {
                Success = false,
                Message = "Unable to upload Passport. Please try again later."
            };
        }
    }


    public async Task<DeletePassportResponseDto> DeletePassportAsync(
        Guid candidateId)
    {
        try
        {
            // =====================================================
            // 1. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new DeletePassportResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 2. Find Latest Passport
            // =====================================================

            var passport = await _context.PassportVerifications
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (passport == null)
            {
                return new DeletePassportResponseDto
                {
                    Success = false,
                    Message = "Passport record not found."
                };
            }

            // Store Cloudinary Public IDs before deleting
            var frontPublicId = passport.FrontPublicId;
            var backPublicId = passport.BackPublicId;

            // =====================================================
            // 3. Remove Passport Record
            // =====================================================

            _context.PassportVerifications.Remove(passport);

            // =====================================================
            // 4. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 5. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 6. Delete Cloudinary Images
            // Only after successful database save
            // =====================================================

            try
            {
                if (!string.IsNullOrWhiteSpace(frontPublicId))
                {
                    await _fileStorage.DeleteAsync(frontPublicId);
                }

                if (!string.IsNullOrWhiteSpace(backPublicId))
                {
                    await _fileStorage.DeleteAsync(backPublicId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete Passport images from Cloudinary for Candidate {CandidateId}",
                    candidateId);
            }

            // =====================================================
            // 7. Logging
            // =====================================================

            _logger.LogInformation(
                "Passport deleted successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 8. Response
            // =====================================================

            return new DeletePassportResponseDto
            {
                Success = true,
                Message = "Passport deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeletePassportAsync failed for Candidate {CandidateId}",
                candidateId);

            return new DeletePassportResponseDto
            {
                Success = false,
                Message = "Unable to delete passport. Please try again later."
            };
        }
    }
    // ════════════════════════════════════════════════
    // AADHAAR
    // ════════════════════════════════════════════════
    public async Task<UploadAadhaarResponseDto> UploadAadhaarAsync(
      Guid candidateId,
      UploadAadhaarRequestDto request,
      IFormFile frontImage,
      IFormFile? backImage)
    {
        FileUploadResult? frontUpload = null;
        FileUploadResult? backUpload = null;

        try
        {
            // =====================================================
            // 1. Consent Validation
            // =====================================================
            if (!request.ConsentGiven)
            {
                return new UploadAadhaarResponseDto
                {
                    Success = false,
                    Message = "Consent is required to process Aadhaar data."
                };
            }

            // =====================================================
            // 2. Validate Images
            // =====================================================
            var frontError = ValidateFile(
                frontImage,
                AllowedImgTypes,
                MaxImageSizeBytes);

            if (frontError != null)
            {
                return new UploadAadhaarResponseDto
                {
                    Success = false,
                    Message = frontError
                };
            }

            if (backImage != null)
            {
                var backError = ValidateFile(
                    backImage,
                    AllowedImgTypes,
                    MaxImageSizeBytes);

                if (backError != null)
                {
                    return new UploadAadhaarResponseDto
                    {
                        Success = false,
                        Message = backError
                    };
                }
            }

            // =====================================================
            // 3. Load Candidate Profile
            // =====================================================
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new UploadAadhaarResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 4. Find Existing Aadhaar
            // =====================================================
            var existingKyc = await _context.KycVerifications
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId &&
                    x.IdType == "Aadhaar");

            // =====================================================
            // 5. Upload Front Image to Cloudinary
            // =====================================================
            frontUpload = await _fileStorage.UploadDocumentAsync(
                frontImage,
                "aadhaar/front");

            // =====================================================
            // 6. Upload Back Image (Optional)
            // =====================================================
            if (backImage != null)
            {
                backUpload = await _fileStorage.UploadDocumentAsync(
                    backImage,
                    "aadhaar/back");
            }

            // =====================================================
            // Part 3.1B Starts Here
            // Gemini OCR Parsing
            // =====================================================

            // =====================================================
            // 7. Parse Aadhaar Using Gemini OCR
            // =====================================================
            _logger.LogInformation(
                "Sending Aadhaar to Gemini OCR for Candidate {CandidateId}",
                candidateId);

            var parseResult =
                await _geminiDocumentParserService.ParseDocumentAsync(frontImage);

            if (!parseResult.Success)
            {
                _logger.LogWarning(
                    "Gemini OCR failed for Candidate {CandidateId}. Error: {Error}",
                    candidateId,
                    parseResult.Message);
            }

            // =====================================================
            // 8. Read OCR Fields
            // =====================================================
            string? extractedName = null;
            DateOnly? extractedDob = null;
            string? extractedAddress = null;

            if (parseResult.Success &&
                parseResult.ParsedData.HasValue)
            {
                var fields = parseResult.ParsedData.Value;

                if (fields.TryGetProperty("name", out var name))
                    extractedName = name.GetString();

                if (fields.TryGetProperty("dob", out var dob))
                {
                    if (DateOnly.TryParse(dob.GetString(), out var parsedDob))
                        extractedDob = parsedDob;
                }

                if (fields.TryGetProperty("address", out var address))
                    extractedAddress = address.GetString();
            }

            // =====================================================
            // 9. Create KYC Verification
            // =====================================================
            var verification = new KycVerification
            {
                VerificationId = Guid.NewGuid(),

                CandidateId = candidateId,

                IdType = "Aadhaar",

                IdFrontImageUrl = frontUpload.Url,
                IdFrontPublicId = frontUpload.PublicId,

                IdBackImageUrl = backUpload?.Url,
                IdBackPublicId = backUpload?.PublicId,

                AiExtractedName = extractedName,
                AiExtractedDob = extractedDob,
                AiExtractedAddress = extractedAddress,

                //If later you enhance the parser to calculate or receive confidence,
                //you can simply change these lines to:
                //AiConfidenceScore = parseResult.AiConfidenceScore,

                //OcrConfidence = parseResult.OcrConfidence,
                AiConfidenceScore = null,

                OcrConfidence = null,

                IdHash = Guid.NewGuid().ToString(),

                AdminDecision = "Pending",

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existingKyc != null)
            {
                _context.KycVerifications.Remove(existingKyc);
            }

            _context.KycVerifications.Add(verification);
            // =====================================================
            // 10. Auto Fill Candidate Profile
            // Only fill empty fields
            // =====================================================

            if (profile != null)
            {
                if (string.IsNullOrWhiteSpace(profile.FullName) &&
                    !string.IsNullOrWhiteSpace(extractedName))
                {
                    profile.FullName = extractedName;
                }

                if (!profile.DateOfBirth.HasValue &&
                    extractedDob.HasValue)
                {
                    profile.DateOfBirth = extractedDob.Value;
                }

                if (string.IsNullOrWhiteSpace(profile.About) &&
                    !string.IsNullOrWhiteSpace(extractedAddress))
                {
                    profile.About = extractedAddress;
                }

                profile.UpdatedAt = DateTime.UtcNow;
            }
            // =====================================================
            // 11. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            // =====================================================
            // 12. Remove Previous Aadhaar Record
            // =====================================================



            // =====================================================
            // 13. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 14. Delete Previous Cloudinary Images
            // Only after successful DB save
            // =====================================================

            if (existingKyc != null)
            {
                try
                {
                    await _fileStorage.DeleteAsync(
      existingKyc.IdFrontPublicId);

                    await _fileStorage.DeleteAsync(
                        existingKyc.IdBackPublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete previous Aadhaar images for Candidate {CandidateId}",
                        candidateId);
                }
            }

            // =====================================================
            // 15. Logging
            // =====================================================

            _logger.LogInformation(
                "Aadhaar uploaded successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 16. Response
            // =====================================================

            return new UploadAadhaarResponseDto
            {
                Success = true,

                Message = parseResult.Success
                    ? "Aadhaar uploaded and processed successfully."
                    : "Aadhaar uploaded successfully. OCR could not extract all information.",

                VerificationId = verification.VerificationId,

                FrontImageUrl = verification.IdFrontImageUrl,

                BackImageUrl = verification.IdBackImageUrl,

                AdminDecision = verification.AdminDecision,

                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UploadAadhaarAsync failed for Candidate {CandidateId}",
                candidateId);

            return new UploadAadhaarResponseDto
            {
                Success = false,
                Message = "Unable to upload Aadhaar. Please try again later."
            };
        }
    }

    public async Task<DeleteAadhaarResponseDto> DeleteAadhaarAsync(
     Guid candidateId)
    {
        try
        {
            // =====================================================
            // 1. Load Candidate Profile
            // =====================================================

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
            {
                return new DeleteAadhaarResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            // =====================================================
            // 2. Find Latest Aadhaar
            // =====================================================

            var aadhaar = await _context.KycVerifications
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId &&
                    x.IdType == "Aadhaar");

            if (aadhaar == null)
            {
                return new DeleteAadhaarResponseDto
                {
                    Success = false,
                    Message = "Aadhaar record not found."
                };
            }

            // Store Cloudinary PublicIds before deleting
            var frontPublicId = aadhaar.IdFrontPublicId;
            var backPublicId = aadhaar.IdBackPublicId;

            // =====================================================
            // 3. Remove Aadhaar Record
            // =====================================================

            _context.KycVerifications.Remove(aadhaar);

            // =====================================================
            // 4. Update Profile Completion
            // =====================================================

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            profile.ProfileCompletionPct =
                completion?.OverallPct ?? 0;

            profile.UpdatedAt = DateTime.UtcNow;

            // =====================================================
            // 5. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // 6. Delete Cloudinary Images
            // Only after successful database save
            // =====================================================

            try
            {
                if (!string.IsNullOrWhiteSpace(frontPublicId))
                {
                    await _fileStorage.DeleteAsync(frontPublicId);
                }

                if (!string.IsNullOrWhiteSpace(backPublicId))
                {
                    await _fileStorage.DeleteAsync(backPublicId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete Aadhaar images from Cloudinary for Candidate {CandidateId}",
                    candidateId);
            }

            // =====================================================
            // 7. Logging
            // =====================================================

            _logger.LogInformation(
                "Aadhaar deleted successfully for Candidate {CandidateId}",
                candidateId);

            // =====================================================
            // 8. Response
            // =====================================================

            return new DeleteAadhaarResponseDto
            {
                Success = true,
                Message = "Aadhaar deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteAadhaarAsync failed for Candidate {CandidateId}",
                candidateId);

            return new DeleteAadhaarResponseDto
            {
                Success = false,
                Message = "Unable to delete Aadhaar. Please try again later."
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    // PRIVATE — Auto-fill, skills, work history, education
    // ══════════════════════════════════════════════════════════

    private static void AutoFillProfileFields(
       CandidateProfile profile,
       AffindaParseResult result)
    {
        // =====================================================
        // Basic Details
        // Only fill empty fields
        // =====================================================

        if (string.IsNullOrWhiteSpace(profile.PrimaryTrade) &&
            !string.IsNullOrWhiteSpace(result.ParsedTrade))
        {
            profile.PrimaryTrade = result.ParsedTrade;
        }

        if (profile.TotalExperienceYears <= 0 &&
            result.ParsedExperienceYrs.HasValue)
        {
            profile.TotalExperienceYears = result.ParsedExperienceYrs.Value;
        }

        if (string.IsNullOrWhiteSpace(profile.CurrentCity) &&
            !string.IsNullOrWhiteSpace(result.City))
        {
            profile.CurrentCity = result.City;
        }

        if (string.IsNullOrWhiteSpace(profile.CurrentState) &&
            !string.IsNullOrWhiteSpace(result.State))
        {
            profile.CurrentState = result.State;
        }

        if (string.IsNullOrWhiteSpace(profile.Nationality) &&
            !string.IsNullOrWhiteSpace(result.Country))
        {
            profile.Nationality = result.Country;
        }

        // =====================================================
        // About — the single summary field used everywhere in the
        // app (Personal tab, Portal CV, employer views). Only fills
        // it if currently blank, so a resume re-upload never
        // clobbers a summary the candidate has since edited by hand.
        // ProfessionalSummary is intentionally left untouched here —
        // it's a legacy field no longer read anywhere in the app.
        // =====================================================

        if (string.IsNullOrWhiteSpace(profile.About) &&
            !string.IsNullOrWhiteSpace(result.ProfessionalSummary))
        {
            profile.About = result.ProfessionalSummary;
        }

        // =====================================================
        // Audit
        // =====================================================

        profile.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpsertSkillsAsync(
        CandidateProfile profile,
        List<string> affindaSkills,
        Guid candidateId)
    {
        if (affindaSkills == null || !affindaSkills.Any())
            return;

        var existingSkills = await _context.CandidateSkills
            .Where(x => x.CandidateId == candidateId && x.SkillType == "Skill")
            .ToListAsync();

        // If the candidate has manually added/edited skills (anything not
        // tagged as Affinda-derived — a real proficiency level like
        // "Beginner"/"Intermediate"/"Expert" is stored in SkillRole for
        // those), respect their edits and leave them alone.
        if (existingSkills.Any(x => x.SkillRole != "Affinda"))
            return;

        // Otherwise every existing skill here came from a previous resume
        // parse — replace them with the freshly parsed set so re-uploading
        // a resume actually refreshes the profile instead of being silently
        // ignored forever after the first upload.
        if (existingSkills.Any())
            _context.CandidateSkills.RemoveRange(existingSkills);

        var distinctSkills = affindaSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in distinctSkills)
        {
            _context.CandidateSkills.Add(new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = skill,
                SkillType = "Skill",
                SkillRole = "Affinda",
                YearsOfExperience = null,
                CanRead = null,
                CanWrite = null,
                CanSpeak = null
            });
        }
    }


    private async Task UpsertWorkHistoriesAsync(
       CandidateProfile profile,
       List<AffindaWorkExp> affindaWork,
       Guid candidateId)
    {
        if (affindaWork == null || !affindaWork.Any())
            return;

        var existingWork = await _context.CandidateWorkHistories
            .Where(x => x.CandidateId == candidateId)
            .ToListAsync();

        // Manually-added work history is never AI-verified — leave it alone.
        if (existingWork.Any(x => !x.IsAiVerified))
            return;

        // Otherwise everything here came from a previous resume parse —
        // replace with the freshly parsed set.
        if (existingWork.Any())
            _context.CandidateWorkHistories.RemoveRange(existingWork);

        foreach (var exp in affindaWork)
        {
            if (string.IsNullOrWhiteSpace(
                exp.Parsed?.WorkExperienceJobTitle?.Parsed))
            {
                continue;
            }

            var workDates = exp.Parsed?.WorkExperienceDates?.Parsed;

            var startDate =
                ParseDatePoint(workDates?.Start);

            // Affinda sometimes gives no usable date for an entry at all
            // (common on resumes with dates in a separate sidebar column
            // that don't associate cleanly with each role). Previously this
            // silently defaulted to today's date, which is actively
            // misleading for a past job — e.g. "started today". StartDate is
            // now nullable, so we keep the entry (title/company/description
            // are still valuable) and simply leave the date unset rather
            // than inventing one.
            var endDate =
                workDates?.End?.IsCurrent == true
                    ? null
                    : ParseDatePoint(workDates?.End);

            _context.CandidateWorkHistories.Add(new CandidateWorkHistory
            {
                WorkId = Guid.NewGuid(),

                CandidateId = candidateId,

                CompanyName =
                    exp.Parsed?.WorkExperienceOrganization?.Parsed
                    ?? "Unknown Company",

                JobTitle =
                    exp.Parsed?.WorkExperienceJobTitle?.Parsed!,

                StartDate = startDate,

                EndDate = endDate,

                IsCurrent =
                    workDates?.End?.IsCurrent
                    ?? false,

                JobDescription =
                    exp.Parsed?.WorkExperienceDescription?.Parsed,

                WorkLocation =
                    exp.Parsed?.WorkExperienceLocation?.Parsed?.Formatted,

                IsOffshore = false,

                IsAiVerified = true
            });
        }
    }

    private static List<string> MapLanguagesForResponse(List<AffindaLanguage>? languages)
    {
        if (languages == null || !languages.Any()) return new();

        return languages
            .Select(l => l.Parsed?.LanguageName?.Parsed?.Label)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<AiParsedEducationDto> MapEducationForResponse(List<AffindaEducation>? educations)
    {
        if (educations == null || !educations.Any()) return new();

        return educations.Select(edu =>
        {
            var accreditation = edu.Parsed?.EducationAccreditation?.Parsed;
            var levelLabel = edu.Parsed?.EducationLevel?.Value ?? edu.Parsed?.EducationLevel?.Label;
            var eduDates = edu.Parsed?.EducationDates?.Parsed;

            var grade =
                edu.Parsed?.EducationGrade?.EducationGradeScore?.ToString()
                ?? edu.Parsed?.EducationGrade?.GradeScore?.ToString();
            var gradeUnit = edu.Parsed?.EducationGrade?.GradeUnit?.Label;

            return new AiParsedEducationDto
            {
                Qualification = accreditation,
                Level = levelLabel,
                InstituteName = edu.Parsed?.EducationOrganization?.Parsed,
                StartYear = eduDates?.Start?.Year,
                EndYear = eduDates?.End?.Year,
                Grade = string.IsNullOrWhiteSpace(grade)
                    ? null
                    : string.IsNullOrWhiteSpace(gradeUnit) ? grade : $"{grade} {gradeUnit}"
            };
        })
        .Where(e => !string.IsNullOrWhiteSpace(e.Qualification)
                 || !string.IsNullOrWhiteSpace(e.InstituteName)
                 || !string.IsNullOrWhiteSpace(e.Level))
        .ToList();
    }

    private static List<AiParsedWorkExperienceDto> MapWorkExperienceForResponse(List<AffindaWorkExp>? workExperiences)
    {
        if (workExperiences == null || !workExperiences.Any()) return new();

        return workExperiences
            .Where(exp => !string.IsNullOrWhiteSpace(exp.Parsed?.WorkExperienceJobTitle?.Parsed))
            .Select(exp =>
            {
                var workDates = exp.Parsed?.WorkExperienceDates?.Parsed;
                return new AiParsedWorkExperienceDto
                {
                    JobTitle = exp.Parsed?.WorkExperienceJobTitle?.Parsed,
                    CompanyName = exp.Parsed?.WorkExperienceOrganization?.Parsed,
                    Location = exp.Parsed?.WorkExperienceLocation?.Parsed?.Formatted,
                    StartDate = ParseDatePoint(workDates?.Start),
                    EndDate = workDates?.End?.IsCurrent == true
                        ? null
                        : ParseDatePoint(workDates?.End),
                    IsCurrent = workDates?.End?.IsCurrent ?? false,
                    Description = exp.Parsed?.WorkExperienceDescription?.Parsed
                };
            })
            .ToList();
    }

    private async Task UpsertLanguagesAsync(
        List<AffindaLanguage> affindaLanguages,
        Guid candidateId)
    {
        if (affindaLanguages == null || !affindaLanguages.Any())
            return;

        var existingLanguages = await _context.CandidateSkills
            .Where(x => x.CandidateId == candidateId && x.SkillType == "Language")
            .ToListAsync();

        // Manually-added languages carry a real proficiency value in
        // SkillRole (e.g. "Conversational") rather than "Affinda" — leave
        // those alone.
        if (existingLanguages.Any(x => x.SkillRole != "Affinda"))
            return;

        // Otherwise these are all left over from a previous resume parse —
        // replace with the freshly parsed set.
        if (existingLanguages.Any())
            _context.CandidateSkills.RemoveRange(existingLanguages);

        var distinctLanguages = affindaLanguages
            .Select(l => l.Parsed?.LanguageName?.Parsed?.Label)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var languageName in distinctLanguages)
        {
            _context.CandidateSkills.Add(new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = languageName,
                SkillType = "Language",
                SkillRole = "Affinda",
                YearsOfExperience = null,
                CanRead = null,
                CanWrite = null,
                CanSpeak = null
            });
        }
    }

    private async Task UpsertEducationsAsync(
        List<AffindaEducation> affindaEducations,
        Guid candidateId)
    {
        if (affindaEducations == null || !affindaEducations.Any())
            return;

        var existingEducations = await _context.CandidateEducations
            .Where(x => x.CandidateId == candidateId)
            .ToListAsync();

        // Manually-added education entries are not AI-verified — leave
        // those alone and don't touch this candidate's education at all.
        if (existingEducations.Any(x => !x.IsAiVerified))
            return;

        // Otherwise every existing row here came from a previous resume
        // parse — replace with the freshly parsed set.
        if (existingEducations.Any())
            _context.CandidateEducations.RemoveRange(existingEducations);

        foreach (var edu in affindaEducations)
        {
            var accreditation = edu.Parsed?.EducationAccreditation?.Parsed;
            var organization = edu.Parsed?.EducationOrganization?.Parsed;
            var levelLabel = edu.Parsed?.EducationLevel?.Value ?? edu.Parsed?.EducationLevel?.Label;

            // Previously this skipped the ENTIRE record (institute, year, everything)
            // whenever Affinda didn't confidently extract a degree/accreditation title.
            // That silently dropped vocational / trade / school-board entries (e.g.
            // "ITI - Electrician Trade", "SSC (10th Standard)") where Affinda's resume
            // model is less reliable at naming the qualification but still correctly
            // extracts the institute and dates. Only skip if we have NOTHING usable —
            // no accreditation, no organization, and no level — since at that point
            // there's nothing meaningful to save anyway.
            if (string.IsNullOrWhiteSpace(accreditation) &&
                string.IsNullOrWhiteSpace(organization) &&
                string.IsNullOrWhiteSpace(levelLabel))
            {
                continue;
            }

            var level = MapEducationLevel(levelLabel);

            short? passoutYear = null;

            if (edu.Parsed?.EducationDates?.Parsed?.End?.Year != null)
            {
                passoutYear =
                    (short)edu.Parsed.EducationDates.Parsed.End.Year.Value;
            }

            var grade =
                edu.Parsed?.EducationGrade?.EducationGradeScore?.ToString()
                ?? edu.Parsed?.EducationGrade?.GradeScore?.ToString();

            var gradeUnit =
                edu.Parsed?.EducationGrade?.GradeUnit?.Label;

            _context.CandidateEducations.Add(new CandidateEducation
            {
                EducationId = Guid.NewGuid(),

                CandidateId = candidateId,

                // Fall back to the accreditation text itself, or the level label,
                // rather than losing the qualification name entirely.
                EducationLevel = !string.IsNullOrWhiteSpace(accreditation)
                    ? accreditation
                    : level,

                InstituteName = organization ?? "Unknown Institute",

                PassoutYear = passoutYear,

                YearDetails = string.IsNullOrWhiteSpace(grade)
                    ? null
                    : string.IsNullOrWhiteSpace(gradeUnit)
                        ? grade
                        : $"{grade} {gradeUnit}",

                CertificateUrl = null,

                CertificateNumber = null,

                IsAiVerified = true,

                CreatedAt = DateTime.UtcNow
            });
        }
    }
    // ── Helpers ──────────────────────────────────────────────

    private static DateOnly? ParseDatePoint(AffindaDatePoint? point)
    {
        if (point == null) return null;
        if (point.Year.HasValue)
        {
            try { return new DateOnly(point.Year.Value, point.Month ?? 1, point.Day ?? 1); }
            catch { return new DateOnly(point.Year.Value, 1, 1); }
        }
        if (!string.IsNullOrWhiteSpace(point.Date) && DateOnly.TryParse(point.Date, out var parsed))
            return parsed;
        return null;
    }

    private static string MapEducationLevel(string? level) => level switch
    {
        "Bachelor" => "Graduate",
        "Master" => "Post Graduate",
        "Doctorate" => "Post Graduate",
        "Diploma" => "Diploma",
        "Course/Certificate" => "ITI",
        "High School" => "12th",
        _ => level ?? "Other"
    };

    private static string? ValidateFile(IFormFile? file, string[] allowedTypes, long maxBytes)
    {
        if (file == null || file.Length == 0) return "No file provided.";
        if (file.Length > maxBytes) return $"File size must not exceed {maxBytes / 1024 / 1024} MB.";
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return $"Unsupported file type. Allowed: {string.Join(", ", allowedTypes)}.";
        return null;
    }

    private static byte RecalcPct(CandidateProfile profile)
    {
        int completed = 0;
        const int totalSections = 10;

        // ============================================
        // 1. Basic Profile
        // ============================================
        if (!string.IsNullOrWhiteSpace(profile.FullName))
            completed++;

        // ============================================
        // 2. Personal Details
        // ============================================
        if (profile.DateOfBirth.HasValue &&
            !string.IsNullOrWhiteSpace(profile.Gender))
            completed++;

        // ============================================
        // 3. Location
        // ============================================
        if (!string.IsNullOrWhiteSpace(profile.CurrentCity) &&
            !string.IsNullOrWhiteSpace(profile.CurrentState))
            completed++;

        // ============================================
        // 4. Resume
        // ============================================
        if (profile.Cvs?.Any(x => !string.IsNullOrWhiteSpace(x.CvFileUrl)) == true)
            completed++;

        // ============================================
        // 5. Skills
        // ============================================
        if (profile.Skills?.Any() == true)
            completed++;

        // ============================================
        // 6. Work History
        // ============================================
        if (profile.WorkHistories?.Any() == true)
            completed++;

        // ============================================
        // 7. Education
        // ============================================
        if (profile.Educations?.Any() == true)
            completed++;

        // ============================================
        // 8. Professional Information
        // ============================================
        if (!string.IsNullOrWhiteSpace(profile.PrimaryTrade) &&
            profile.TotalExperienceYears >= 0)
            completed++;

        // ============================================
        // 9. About
        // ============================================
        if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary) ||
            !string.IsNullOrWhiteSpace(profile.About))
            completed++;

        // ============================================
        // 10. Documents
        // ============================================
        if ((profile.KycVerifications?.Any() ?? false) ||
            (profile.PassportVerifications?.Any() ?? false))
            completed++;

        return (byte)Math.Round((double)completed * 100 / totalSections);
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
        ParsedSkills = cv.ParsedSkillsJson,
        AiConfidenceScore = cv.AiConfidenceScore,
        UploadedAt = cv.GeneratedAt,
        VerificationStatus = "Pending"
    };

    private static byte CalculateCompletionPctDetailed(
     bool photo,
     bool personal,
     bool summary,
     bool resume,
     bool edu,
     bool work,
     bool skills,
     bool aadhaar,
     bool passport)
    {
        int score = 0;

        if (photo) score += 15;
        if (personal) score += 15;
        if (summary) score += 10;
        if (resume) score += 20;
        if (edu) score += 10;
        if (work) score += 10;
        if (skills) score += 10;
        if (aadhaar) score += 5;
        if (passport) score += 5;

        return (byte)Math.Min(score, 100);
    }
    private async Task<ProfileCompletionData> BuildProfileCompletionDataAsync(Guid candidateId)
    {
        var profile = await _context.CandidateProfiles
            .Include(x => x.Cvs)
            .Include(x => x.Educations)
            .Include(x => x.WorkHistories)
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile == null)
            return null!;

        var hasAadhaar = await _context.KycVerifications
            .AnyAsync(x => x.CandidateId == candidateId);

        var hasPassport = await _context.PassportVerifications
            .AnyAsync(x => x.CandidateId == candidateId);

        var hasPhoto =
            !string.IsNullOrWhiteSpace(profile.ProfilePhotoUrl);

        var hasPersonal =
            !string.IsNullOrWhiteSpace(profile.FullName) &&
            profile.DateOfBirth.HasValue &&
            !string.IsNullOrWhiteSpace(profile.CurrentCity) &&
            !string.IsNullOrWhiteSpace(profile.CurrentState);

        var hasSummary =
            !string.IsNullOrWhiteSpace(profile.About) ||
            !string.IsNullOrWhiteSpace(profile.ProfessionalSummary);

        var hasResume =
            profile.Cvs.Any(x =>
                !string.IsNullOrWhiteSpace(x.CvFileUrl));

        var hasEducation =
            profile.Educations.Any();

        var hasWorkHistory =
            profile.WorkHistories.Any();

        var hasSkills =
            profile.Skills.Any();

        var pending = new List<string>();

        if (!hasPhoto)
            pending.Add("Upload a profile photo");

        if (!hasPersonal)
            pending.Add("Complete personal information");

        if (!hasSummary)
            pending.Add("Add professional summary");

        if (!hasResume)
            pending.Add("Upload your resume");

        if (!hasEducation)
            pending.Add("Add education details");

        if (!hasWorkHistory)
            pending.Add("Add work experience");

        if (!hasSkills)
            pending.Add("Add your skills");

        if (!hasAadhaar)
            pending.Add("Upload Aadhaar for KYC verification");

        if (!hasPassport)
            pending.Add("Upload passport details");

        return new ProfileCompletionData
        {
            OverallPct = CalculateCompletionPctDetailed(
                hasPhoto,
                hasPersonal,
                hasSummary,
                hasResume,
                hasEducation,
                hasWorkHistory,
                hasSkills,
                hasAadhaar,
                hasPassport),

            HasPhoto = hasPhoto,
            HasPersonalInfo = hasPersonal,
            HasSummary = hasSummary,
            HasResume = hasResume,
            HasEducation = hasEducation,
            HasWorkHistory = hasWorkHistory,
            HasSkills = hasSkills,
            HasAadhaar = hasAadhaar,
            HasPassport = hasPassport,
            PendingActions = pending
        };
    }

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

        AiConfidenceScore = k.AiConfidenceScore,

        AiExtractedDocumentNumber = k.AiExtractedDocumentNumber,

        AiExtractedGender = k.AiExtractedGender,

        OcrConfidence = k.OcrConfidence,

        AdminDecision = k.AdminDecision,

        RejectionReason = k.RejectionReason,

        UploadedAt = k.CreatedAt,

        ParsedSuccessfully = !string.IsNullOrWhiteSpace(k.AiExtractedName)
    };

    public async Task<List<CandidateUploadedDocumentDto>> GetUploadedDocumentsAsync(
        Guid candidateId)
    {
        return await _context.CandidateDocuments
            .AsNoTracking()
            .Where(d => d.CandidateId == candidateId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new CandidateUploadedDocumentDto
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileUrl = d.FileUrl,
                ParsedName = d.ParsedName,
                VerificationStatus = d.VerificationStatus,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteUploadedDocumentAsync(
        Guid candidateId,
        Guid documentId)
    {
        var doc = await _context.CandidateDocuments
            .FirstOrDefaultAsync(d =>
                d.DocumentId == documentId &&
                d.CandidateId == candidateId);

        if (doc == null)
            return false;

        _context.CandidateDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        await SafeDeleteUploadAsync(doc.FilePublicId);
        return true;
    }

    private static CandidateDocumentsResponseDto DocsFail(string msg)
        => new() { Success = false, Message = msg };
}