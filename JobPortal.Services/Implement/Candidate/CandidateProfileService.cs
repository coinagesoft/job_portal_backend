
using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
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
    private readonly ICloudinaryService _cloudinaryService;

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
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
                return SummaryFail("Candidate profile not found.");

            var completion =
                await BuildProfileCompletionDataAsync(candidateId);

            return new CandidateProfileSummaryResponseDto
            {
                Success = true,
                Message = "Profile summary retrieved.",

                Data = new CandidateProfileSummaryData
                {
                    CandidateId = profile.CandidateId,

                    FullName = profile.FullName,

                    Role = profile.Role,

                    ProfilePhotoUrl = profile.ProfilePhotoUrl,

                    MobileNumber = profile.User?.MobileNumber,

                    CountryCode = profile.User?.CountryCode,

                    Email = profile.User?.Email,

                    CurrentCity = profile.CurrentCity,

                    CurrentState = profile.CurrentState,

                    TotalExperienceYears = profile.TotalExperienceYears,

                    NoticePeriod = profile.NoticePeriod,

                    About = profile.About,

                    AvailabilityStatus = profile.AvailabilityStatus,

                    ProfessionalSummary = profile.ProfessionalSummary,

                    ProfileCompletionPct = completion.OverallPct
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetProfileSummaryAsync failed for {CandidateId}",
                candidateId);

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
                    Role                 = profile.Role,
                    ProfilePhotoUrl      = profile.ProfilePhotoUrl,
                    DateOfBirth          = profile.DateOfBirth,
                    Gender               = profile.Gender,
                    Email                = profile.User?.Email,
                    MobileNumber         = profile.User?.MobileNumber,
                    CountryCode          = profile.User?.CountryCode,
                    CurrentCity          = profile.CurrentCity,
                    CurrentState         = profile.CurrentState,
                    Pincode              = profile.Pincode,   // add Pincode field to entity via migration
                    ProfessionalSummary  = profile.ProfessionalSummary,   // add ProfessionalSummary field to entity via migration
                    About                = profile.About,   // add About field to entity via migration
                    NoticePeriod         = profile.NoticePeriod,   // add NoticePeriod field to entity via migration
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

            // ===========================
            // Candidate Profile
            // ===========================

            profile.FullName = request.FullName;
            profile.Role = request.Role;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender;
            profile.CurrentCity = request.CurrentCity;
            profile.CurrentState = request.CurrentState;
            profile.Pincode = request.Pincode;
            profile.ProfessionalSummary = request.ProfessionalSummary;
            profile.About = request.About;
            profile.NoticePeriod = request.NoticePeriod;
            profile.TotalExperienceYears = request.TotalExperienceYears;
            profile.NewsletterOptIn = request.NewsletterOptIn;
            profile.UpdatedAt = DateTime.UtcNow;

            // ===========================
            // User Entity
            // ===========================

            if (profile.User != null)
            {
                // Email
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var emailInUse = await _context.Users.AnyAsync(u =>
                        u.Email == request.Email &&
                        u.UserId != profile.UserId);

                    if (emailInUse)
                        return UpdateFail("This email is already in use by another account.");

                    profile.User.Email = request.Email;
                }

                // Mobile Number
                if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    profile.User.MobileNumber = request.MobileNumber;
                }

                // Country Code
                if (!string.IsNullOrWhiteSpace(request.CountryCode))
                {
                    profile.User.CountryCode = request.CountryCode;
                }

                profile.User.UpdatedAt = DateTime.UtcNow;
            }

            // ===========================
            // Completion %
            // ===========================

            profile.ProfileCompletionPct = CalculateCompletionPct(profile);

            await _context.SaveChangesAsync();

            return new UpdateCandidatePersonalInfoResponseDto
            {
                Success = true,
                Message = "Personal information updated successfully.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpdatePersonalInfoAsync failed for {CandidateId}",
                candidateId);

            return UpdateFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // UPLOAD PROFILE PHOTO
    // ════════════════════════════════════════════════
    public async Task<UploadProfilePhotoResponseDto> UploadProfilePhotoAsync(
      Guid candidateId,
      IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
            {
                return PhotoFail("No file provided.");
            }

            if (photo.Length > MaxFileSizeBytes)
            {
                return PhotoFail("File size must not exceed 5 MB.");
            }

            if (!AllowedImageTypes.Contains(photo.ContentType.ToLower()))
            {
                return PhotoFail(
                    "Only JPEG, PNG, or WebP images are allowed.");
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId);

            if (profile == null)
            {
                return PhotoFail("Candidate profile not found.");
            }

            // Delete old image
            if (!string.IsNullOrWhiteSpace(profile.ProfilePhotoPublicId))
            {
                await _cloudinaryService.DeleteAsync(
                    profile.ProfilePhotoPublicId);
            }

            // Upload new image
            var uploadResult =
                await _cloudinaryService.UploadImageAsync(
                    photo,
                    "candidate-profile-photos");

            profile.ProfilePhotoUrl = uploadResult.Url;
            profile.ProfilePhotoPublicId = uploadResult.PublicId;

            profile.ProfileCompletionPct =
                CalculateCompletionPct(profile);

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UploadProfilePhotoResponseDto
            {
                Success = true,
                Message = "Profile photo uploaded successfully.",
                ProfilePhotoUrl = profile.ProfilePhotoUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UploadProfilePhotoAsync failed for CandidateId:{CandidateId}",
                candidateId);

            return PhotoFail(
                "An error occurred while uploading profile photo.");
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
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId);

            if (profile == null)
            {
                return PhotoFail(
                    "Candidate profile not found.");
            }

            // Delete from Cloudinary
            if (!string.IsNullOrWhiteSpace(
                profile.ProfilePhotoPublicId))
            {
                await _cloudinaryService.DeleteAsync(
                    profile.ProfilePhotoPublicId);
            }

            profile.ProfilePhotoUrl = null;
            profile.ProfilePhotoPublicId = null;

            profile.ProfileCompletionPct =
                CalculateCompletionPct(profile);

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UploadProfilePhotoResponseDto
            {
                Success = true,
                Message = "Profile photo removed."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteProfilePhotoAsync failed for CandidateId:{CandidateId}",
                candidateId);

            return PhotoFail(
                "Internal server error.");
        }
    }

    // ════════════════════════════════════════════════
    // GET PROFILE COMPLETION BREAKDOWN
    // ════════════════════════════════════════════════
    public async Task<ProfileCompletionResponseDto> GetProfileCompletionAsync(
       Guid candidateId)
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
            {
                return new ProfileCompletionResponseDto
                {
                    Success = false,
                    Message = "Profile not found."
                };
            }

            var hasAadhaar = await _context.KycVerifications
                .AnyAsync(k => k.CandidateId == candidateId);

            var hasPassport = await _context.Set<PassportVerification>()
                .AnyAsync(p => p.CandidateId == candidateId);

            var hasPhoto =
                !string.IsNullOrWhiteSpace(profile.ProfilePhotoUrl);

            var hasPersonal =
                !string.IsNullOrWhiteSpace(profile.FullName)
                && profile.DateOfBirth.HasValue
                && !string.IsNullOrWhiteSpace(profile.CurrentCity)
                && !string.IsNullOrWhiteSpace(profile.CurrentState);

            var hasSummary =
                !string.IsNullOrWhiteSpace(profile.About)
                || !string.IsNullOrWhiteSpace(profile.ProfessionalSummary);

            var hasResume =
                profile.Cvs.Any(c =>
                    !string.IsNullOrWhiteSpace(c.CvFileUrl));

            var hasEdu =
                profile.Educations.Any();

            var hasWork =
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

            if (!hasEdu)
                pending.Add("Add education details");

            if (!hasWork)
                pending.Add("Add work experience");

            if (!hasSkills)
                pending.Add("Add your skills");

            if (!hasAadhaar)
                pending.Add("Upload Aadhaar for KYC verification");

            if (!hasPassport)
                pending.Add("Upload passport details");

            var pct = CalculateCompletionPctDetailed(
                hasPhoto,
                hasPersonal,
                hasSummary,
                hasResume,
                hasEdu,
                hasWork,
                hasSkills,
                hasAadhaar,
                hasPassport);

            return new ProfileCompletionResponseDto
            {
                Success = true,
                Message = "Completion data retrieved.",
                Data = new ProfileCompletionData
                {
                    OverallPct = pct,

                    HasPhoto = hasPhoto,

                    HasPersonalInfo = hasPersonal,

                    HasSummary = hasSummary,

                    HasResume = hasResume,

                    HasEducation = hasEdu,

                    HasWorkHistory = hasWork,

                    HasSkills = hasSkills,

                    HasAadhaar = hasAadhaar,

                    HasPassport = hasPassport,

                    PendingActions = pending
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetProfileCompletionAsync failed for {CandidateId}",
                candidateId);

            return new ProfileCompletionResponseDto
            {
                Success = false,
                Message = "Internal server error."
            };
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
                    Message = "User not found."
                };
            }

            var existingProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingProfile != null)
            {
                return new CreateCandidateProfileResponseDto
                {
                    Success = false,
                    Message = "Profile already exists."
                };
            }

            // ============================================
            // Validate Email
            // ============================================

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailExists = await _context.Users.AnyAsync(x =>
                    x.Email == request.Email &&
                    x.UserId != userId);

                if (emailExists)
                {
                    return new CreateCandidateProfileResponseDto
                    {
                        Success = false,
                        Message = "Email already exists."
                    };
                }

                user.Email = request.Email;
            }

            // ============================================
            // Update User Table
            // ============================================

            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                user.MobileNumber = request.MobileNumber;

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
                user.CountryCode = request.CountryCode;

            user.UpdatedAt = DateTime.UtcNow;

            // ============================================
            // Create Candidate Profile
            // ============================================

            var profile = new CandidateProfile
            {
                CandidateId = Guid.NewGuid(),
                UserId = user.UserId,

                FullName = string.IsNullOrWhiteSpace(request.FullName)
                    ? (user.Email ?? "Candidate")
                    : request.FullName,

                Role = request.Role,

                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Nationality = request.Nationality,

                CurrentCity = request.CurrentCity,
                CurrentState = request.CurrentState,
                Pincode = request.Pincode,

                PreferredWorkLocation = request.PreferredWorkLocation,
                PreferredSalary = request.PreferredSalary,

                NoticePeriod = request.NoticePeriod,

                About = request.About,
                ProfessionalSummary = request.ProfessionalSummary,

                DisabilityStatus = request.DisabilityStatus,
                DisabilityNote = request.DisabilityNote,

                PrimaryTrade = request.PrimaryTrade,

                TotalExperienceYears = request.TotalExperienceYears,

                ItiCertified = request.ItiCertified,
                ItiTrade = request.ItiTrade,
                ItiMarks = request.ItiMarks,
                ItiCollege = request.ItiCollege,

                NewsletterOptIn = request.NewsletterOptIn,

                AvailabilityStatus = "Available",
                ProfileStatus = "Active",

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CandidateProfiles.Add(profile);

            // First save so CandidateId exists in DB
            await _context.SaveChangesAsync();

            // ============================================
            // Calculate Profile Completion
            // (Common Helper used everywhere)
            // ============================================

            var completion = await BuildProfileCompletionDataAsync(profile.CandidateId);

            profile.ProfileCompletionPct = completion.OverallPct;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CreateCandidateProfileResponseDto
            {
                Success = true,
                Message = "Profile created successfully.",

                CandidateId = profile.CandidateId,

                ProfileCompletionPct = completion.OverallPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateProfileAsync failed for UserId {UserId}",
                userId);

            return new CreateCandidateProfileResponseDto
            {
                Success = false,
                Message = "Internal server error."
            };
        }
    }
    // ════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════

    private static byte CalculateCompletionPct(CandidateProfile p)
    {
        bool photo =
            !string.IsNullOrWhiteSpace(p.ProfilePhotoUrl);

        bool personal =
            !string.IsNullOrWhiteSpace(p.FullName)
            && p.DateOfBirth.HasValue
            && !string.IsNullOrWhiteSpace(p.CurrentCity)
            && !string.IsNullOrWhiteSpace(p.CurrentState);

        bool summary =
            !string.IsNullOrWhiteSpace(p.About)
            || !string.IsNullOrWhiteSpace(p.ProfessionalSummary);

        bool resume =
            p.Cvs?.Any(x =>
                !string.IsNullOrWhiteSpace(x.CvFileUrl)) == true;

        bool edu =
            p.Educations?.Any() == true;

        bool work =
            p.WorkHistories?.Any() == true;

        bool skills =
            p.Skills?.Any() == true;

        bool aadhaar =
            p.KycVerifications?.Any() == true;

        bool passport =
            p.PassportVerifications?.Any() == true;

        return CalculateCompletionPctDetailed(
            photo,
            personal,
            summary,
            resume,
            edu,
            work,
            skills,
            aadhaar,
            passport);
    }
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

    // ── Fail helpers ──────────────────────────────────────────

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

    private static CandidateProfileSummaryResponseDto SummaryFail(string msg)
        => new() { Success = false, Message = msg };

    private static CandidatePersonalInfoResponseDto PersonalFail(string msg)
        => new() { Success = false, Message = msg };

    private static UpdateCandidatePersonalInfoResponseDto UpdateFail(string msg)
        => new() { Success = false, Message = msg };

    private static UploadProfilePhotoResponseDto PhotoFail(string msg)
        => new() { Success = false, Message = msg };
}
