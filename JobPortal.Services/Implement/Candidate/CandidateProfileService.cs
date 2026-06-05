// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateProfileService.cs
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

public class CandidateProfileService : ICandidateProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateProfileService> _logger;
    private readonly IConfiguration _configuration;

    // Max file size 5 MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes  = { "image/jpeg", "image/png", "image/webp" };

    public CandidateProfileService(
        AppDbContext context,
        ILogger<CandidateProfileService> logger,
        IConfiguration configuration)
    {
        _context       = context;
        _logger        = logger;
        _configuration = configuration;
    }

    // ════════════════════════════════════════════════
    // GET PROFILE SUMMARY
    // ════════════════════════════════════════════════
    public async Task<CandidateProfileSummaryResponseDto> GetProfileSummaryAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return SummaryFail("Candidate profile not found.");

            return new CandidateProfileSummaryResponseDto
            {
                Success = true,
                Message = "Profile summary retrieved.",
                Data = new CandidateProfileSummaryData
                {
                    CandidateId          = profile.CandidateId,
                    FullName             = profile.FullName,
                    ProfilePhotoUrl      = profile.ProfilePhotoUrl,
                    MobileNumber         = profile.User?.MobileNumber,
                    CountryCode          = profile.User?.CountryCode,
                    Email                = profile.User?.Email,
                    CurrentCity          = profile.CurrentCity,
                    CurrentState         = profile.CurrentState,
                    TotalExperienceYears = profile.TotalExperienceYears,
                    NoticePeriod         = profile.PreferredWorkLocation, // re-mapped; add NoticePeriod field via migration if needed
                    About                = null,                          // add About field to entity via migration
                    ProfileCompletionPct = profile.ProfileCompletionPct,
                    AvailabilityStatus   = profile.AvailabilityStatus
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProfileSummaryAsync failed for {CandidateId}", candidateId);
            return SummaryFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // GET PERSONAL INFO
    // ════════════════════════════════════════════════
    public async Task<CandidatePersonalInfoResponseDto> GetPersonalInfoAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return PersonalFail("Candidate profile not found.");

            return new CandidatePersonalInfoResponseDto
            {
                Success = true,
                Message = "Personal info retrieved.",
                Data = new CandidatePersonalInfoData
                {
                    CandidateId          = profile.CandidateId,
                    FullName             = profile.FullName,
                    ProfilePhotoUrl      = profile.ProfilePhotoUrl,
                    DateOfBirth          = profile.DateOfBirth,
                    Gender               = profile.Gender,
                    Email                = profile.User?.Email,
                    MobileNumber         = profile.User?.MobileNumber,
                    CountryCode          = profile.User?.CountryCode,
                    CurrentCity          = profile.CurrentCity,
                    CurrentState         = profile.CurrentState,
                    Pincode              = null,   // add Pincode field to entity via migration
                    ProfessionalSummary  = null,   // add ProfessionalSummary field to entity via migration
                    About                = null,   // add About field to entity via migration
                    NoticePeriod         = null,   // add NoticePeriod field to entity via migration
                    TotalExperienceYears = profile.TotalExperienceYears,
                    NewsletterOptIn      = profile.NewsletterOptIn,
                    ProfileCompletionPct = profile.ProfileCompletionPct
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPersonalInfoAsync failed for {CandidateId}", candidateId);
            return PersonalFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // UPDATE PERSONAL INFO
    // ════════════════════════════════════════════════
    public async Task<UpdateCandidatePersonalInfoResponseDto> UpdatePersonalInfoAsync(
        Guid candidateId,
        UpdateCandidatePersonalInfoRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return UpdateFail("Candidate profile not found.");

            // Update fields
            profile.FullName             = request.FullName;
            profile.DateOfBirth          = request.DateOfBirth;
            profile.Gender               = request.Gender;
            profile.CurrentCity          = request.CurrentCity;
            profile.CurrentState         = request.CurrentState;
            profile.TotalExperienceYears = request.TotalExperienceYears;
            profile.NewsletterOptIn      = request.NewsletterOptIn;
            profile.UpdatedAt            = DateTime.UtcNow;

            // Update email on User entity if provided
            if (!string.IsNullOrWhiteSpace(request.Email) && profile.User != null)
            {
                // Check uniqueness
                var emailInUse = await _context.Users.AnyAsync(u =>
                    u.Email == request.Email && u.UserId != profile.UserId);
                if (emailInUse)
                    return UpdateFail("This email is already in use by another account.");

                profile.User.Email     = request.Email;
                profile.User.UpdatedAt = DateTime.UtcNow;
            }

            // NOTE: Pincode, ProfessionalSummary, About, NoticePeriod require new columns
            //       on CandidateProfile entity. Once the migration is added, uncomment:
            // profile.Pincode             = request.Pincode;
            // profile.ProfessionalSummary = request.ProfessionalSummary;
            // profile.About               = request.About;
            // profile.NoticePeriod        = request.NoticePeriod;

            // Recalculate completion %
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);

            await _context.SaveChangesAsync();

            return new UpdateCandidatePersonalInfoResponseDto
            {
                Success              = true,
                Message              = "Personal info updated successfully.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePersonalInfoAsync failed for {CandidateId}", candidateId);
            return UpdateFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // UPLOAD PROFILE PHOTO
    // ════════════════════════════════════════════════
    public async Task<UploadProfilePhotoResponseDto> UploadProfilePhotoAsync(
        Guid candidateId, IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
                return PhotoFail("No file provided.");

            if (photo.Length > MaxFileSizeBytes)
                return PhotoFail("File size must not exceed 5 MB.");

            if (!AllowedImageTypes.Contains(photo.ContentType.ToLower()))
                return PhotoFail("Only JPEG, PNG, or WebP images are allowed.");

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return PhotoFail("Candidate profile not found.");

            // ── Upload to cloud storage (S3 / Firebase Storage) ──────────
            // Replace the block below with your actual storage service call.
            // Example (AWS S3 via AWSSDK):
            //   var url = await _storageService.UploadAsync(
            //       $"profiles/{candidateId}/photo_{Guid.NewGuid()}", photo);
            // For now we use a placeholder URL pattern:
            var fileName  = $"profiles/{candidateId}/photo_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var photoUrl  = $"{_configuration["Storage:BaseUrl"]}/{fileName}";
            // ─────────────────────────────────────────────────────────────

            profile.ProfilePhotoUrl      = photoUrl;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt            = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UploadProfilePhotoResponseDto
            {
                Success         = true,
                Message         = "Profile photo uploaded.",
                ProfilePhotoUrl = photoUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadProfilePhotoAsync failed for {CandidateId}", candidateId);
            return PhotoFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // DELETE PROFILE PHOTO
    // ════════════════════════════════════════════════
    public async Task<UploadProfilePhotoResponseDto> DeleteProfilePhotoAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return PhotoFail("Candidate profile not found.");

            // Optional: delete from storage service here

            profile.ProfilePhotoUrl      = null;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt            = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UploadProfilePhotoResponseDto
            {
                Success = true,
                Message = "Profile photo removed."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteProfilePhotoAsync failed for {CandidateId}", candidateId);
            return PhotoFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // GET PROFILE COMPLETION BREAKDOWN
    // ════════════════════════════════════════════════
    public async Task<ProfileCompletionResponseDto> GetProfileCompletionAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Cvs)
                .Include(p => p.Educations)
                .Include(p => p.WorkHistories)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new ProfileCompletionResponseDto { Success = false, Message = "Profile not found." };

            var hasAadhaar = await _context.KycVerifications
                .AnyAsync(k => k.CandidateId == candidateId);

            var hasPassport = await _context.Set<PassportVerification>()
                .AnyAsync(p => p.CandidateId == candidateId);

            var hasPhoto    = !string.IsNullOrWhiteSpace(profile.ProfilePhotoUrl);
            var hasPersonal = !string.IsNullOrWhiteSpace(profile.FullName)
                              && !string.IsNullOrWhiteSpace(profile.CurrentCity);
            var hasResume   = profile.Cvs.Any(c => !string.IsNullOrWhiteSpace(c.CvFileUrl));
            var hasEdu      = profile.Educations.Any();
            var hasWork     = profile.WorkHistories.Any();
            var hasSkills   = profile.Skills.Any();

            var pending = new List<string>();
            if (!hasPhoto)    pending.Add("Upload a profile photo");
            if (!hasPersonal) pending.Add("Complete personal info (city, DOB)");
            if (!hasResume)   pending.Add("Upload your resume");
            if (!hasEdu)      pending.Add("Add education details");
            if (!hasWork)     pending.Add("Add work experience");
            if (!hasSkills)   pending.Add("Add your skills");
            if (!hasAadhaar)  pending.Add("Upload Aadhaar for KYC verification");

            var pct = CalculateCompletionPctDetailed(
                hasPhoto, hasPersonal, hasResume, hasEdu, hasWork, hasSkills, hasAadhaar, hasPassport);

            return new ProfileCompletionResponseDto
            {
                Success = true,
                Message = "Completion data retrieved.",
                Data = new ProfileCompletionData
                {
                    OverallPct      = pct,
                    HasPhoto        = hasPhoto,
                    HasPersonalInfo = hasPersonal,
                    HasSummary      = false,   // wire to About/ProfessionalSummary once migrated
                    HasResume       = hasResume,
                    HasEducation    = hasEdu,
                    HasWorkHistory  = hasWork,
                    HasSkills       = hasSkills,
                    HasAadhaar      = hasAadhaar,
                    HasPassport     = hasPassport,
                    PendingActions  = pending
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProfileCompletionAsync failed for {CandidateId}", candidateId);
            return new ProfileCompletionResponseDto { Success = false, Message = "Internal server error." };
        }
    }
    public async Task<CreateCandidateProfileResponseDto> CreateProfileAsync(
    Guid userId,
    CreateCandidateProfileRequestDto request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                return new CreateCandidateProfileResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var existingProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingProfile != null)
            {
                return new CreateCandidateProfileResponseDto
                {
                    Success = false,
                    Message = "Profile already exists"
                };
            }

            var profile = new CandidateProfile
            {
                CandidateId = Guid.NewGuid(),
                UserId = user.UserId,

                FullName = user.Email ?? "Candidate",

                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Nationality = request.Nationality,
                CurrentCity = request.CurrentCity,
                CurrentState = request.CurrentState,
                PreferredWorkLocation = request.PreferredWorkLocation,
                PreferredSalary = request.PreferredSalary,
                DisabilityStatus = request.DisabilityStatus,
                DisabilityNote = request.DisabilityNote,
                PrimaryTrade = request.PrimaryTrade,
                TotalExperienceYears = request.TotalExperienceYears,
                ItiCertified = request.ItiCertified,
                ItiTrade = request.ItiTrade,
                ItiMarks = request.ItiMarks,
                ItiCollege = request.ItiCollege,
                NewsletterOptIn = request.NewsletterOptIn,

                Pincode = request.Pincode,
                About = request.About,
                NoticePeriod = request.NoticePeriod,
                ProfessionalSummary = request.ProfessionalSummary,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProfileCompletionPct = 25
            };

            _context.CandidateProfiles.Add(profile);

            await _context.SaveChangesAsync();

            return new CreateCandidateProfileResponseDto
            {
                Success = true,
                Message = "Profile created successfully",
                CandidateId = profile.CandidateId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProfileAsync");

            return new CreateCandidateProfileResponseDto
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }
    // ════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════

    private static byte CalculateCompletionPct(CandidateProfile p)
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

    private static byte CalculateCompletionPctDetailed(
        bool photo, bool personal, bool resume,
        bool edu, bool work, bool skills,
        bool aadhaar, bool passport)
    {
        int score = 0;
        if (photo)    score += 15;
        if (personal) score += 15;
        if (resume)   score += 20;
        if (edu)      score += 10;
        if (work)     score += 10;
        if (skills)   score += 10;
        if (aadhaar)  score += 10;
        if (passport) score += 10;
        return (byte)Math.Min(score, 100);
    }

    // ── Fail helpers ──────────────────────────────────────────

    private static CandidateProfileSummaryResponseDto SummaryFail(string msg)
        => new() { Success = false, Message = msg };

    private static CandidatePersonalInfoResponseDto PersonalFail(string msg)
        => new() { Success = false, Message = msg };

    private static UpdateCandidatePersonalInfoResponseDto UpdateFail(string msg)
        => new() { Success = false, Message = msg };

    private static UploadProfilePhotoResponseDto PhotoFail(string msg)
        => new() { Success = false, Message = msg };
}
