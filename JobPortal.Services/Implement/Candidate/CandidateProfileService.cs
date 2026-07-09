// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateProfileService.cs
//  Implements ICandidateProfileService (profile summary, personal-info,
//  profile photo, completion, create).
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.AI;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IUploads;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<CandidateProfileService> _logger;

    private readonly IEmbeddingStorageService _embeddingStorage;  


    public CandidateProfileService(
        AppDbContext context,
         ILogger<CandidateProfileService> logger,
        IFileStorageService fileStorage,
          IEmbeddingStorageService embeddingStorage)
    {
        _context = context;
        _logger = logger;
        _fileStorage = fileStorage;
        _embeddingStorage = embeddingStorage;
    }

    // ============================================================
    // SUMMARY
    // ============================================================
    public async Task<CandidateProfileSummaryResponseDto> GetProfileSummaryAsync(Guid candidateId)
    {
        var c = await _context.CandidateProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        return new CandidateProfileSummaryResponseDto
        {
            Success = true,
            Message = "Profile summary loaded.",
            Data = new CandidateProfileSummaryData
            {
                CandidateId = c.CandidateId,
                FullName = c.FullName,
                Role = c.Role,
                ProfilePhotoUrl = c.ProfilePhotoUrl,
                MobileNumber = c.User?.MobileNumber,
                CountryCode = c.User?.CountryCode,
                Email = c.User?.Email,
                CurrentCity = c.CurrentCity,
                CurrentState = c.CurrentState,
                TotalExperienceYears = c.TotalExperienceYears,
                NoticePeriod = c.NoticePeriod,
                About = c.About,
                ProfessionalSummary = c.ProfessionalSummary,
                ProfileCompletionPct = c.ProfileCompletionPct,
                AvailabilityStatus = c.AvailabilityStatus
            }
        };
    }

    // ============================================================
    // PERSONAL INFO (GET)
    // ============================================================
    public async Task<CandidatePersonalInfoResponseDto> GetPersonalInfoAsync(Guid candidateId)
    {
        var c = await _context.CandidateProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        return new CandidatePersonalInfoResponseDto
        {
            Success = true,
            Message = "Personal info loaded.",
            Data = new CandidatePersonalInfoData
            {
                CandidateId = c.CandidateId,
                FullName = c.FullName,
                Role = c.Role ?? string.Empty,
                ProfilePhotoUrl = c.ProfilePhotoUrl,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                Email = c.User?.Email,
                MobileNumber = c.User?.MobileNumber,
                CountryCode = c.User?.CountryCode,
                CurrentCity = c.CurrentCity,
                CurrentState = c.CurrentState,
                Pincode = c.Pincode,
                ProfessionalSummary = c.ProfessionalSummary,
                About = c.About,
                NoticePeriod = c.NoticePeriod,
                TotalExperienceYears = c.TotalExperienceYears,
                ExpectedSalary = c.PreferredSalary,
                Nationality = c.Nationality,
                CurrentlyAvailableForWork =
                    string.Equals(c.AvailabilityStatus, "Available",
                        StringComparison.OrdinalIgnoreCase),
                NewsletterOptIn = c.NewsletterOptIn,
                ProfileCompletionPct = c.ProfileCompletionPct
            }
        };
    }

    // ============================================================
    // PERSONAL INFO (UPDATE)
    // ============================================================
    public async Task<UpdateCandidatePersonalInfoResponseDto> UpdatePersonalInfoAsync(
        Guid candidateId, UpdateCandidatePersonalInfoRequestDto r)
    {
        var c = await _context.CandidateProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        if (!string.IsNullOrWhiteSpace(r.FullName)) c.FullName = r.FullName;
        if (!string.IsNullOrWhiteSpace(r.Role))
        {
            c.Role = r.Role;
            // Employer-facing pages (CV Search, applicant cards, AI matching)
            // read PrimaryTrade, not Role. Keep them in sync so a candidate's
            // "Trade / Job Title" edit on the Personal tab is actually reflected
            // there — otherwise PrimaryTrade stays stuck at whatever value was
            // set at registration or parsed from an old resume.
            c.PrimaryTrade = r.Role;
        }
        c.DateOfBirth = r.DateOfBirth ?? c.DateOfBirth;
        c.Gender = r.Gender ?? c.Gender;
        c.CurrentCity = r.CurrentCity ?? c.CurrentCity;
        c.CurrentState = r.CurrentState ?? c.CurrentState;
        c.Pincode = r.Pincode ?? c.Pincode;
        c.ProfessionalSummary = r.ProfessionalSummary ?? c.ProfessionalSummary;
        c.About = r.About ?? c.About;
        c.TotalExperienceYears = r.TotalExperienceYears;
        if (r.ExpectedSalary.HasValue) c.PreferredSalary = r.ExpectedSalary;
        if (!string.IsNullOrWhiteSpace(r.Nationality)) c.Nationality = r.Nationality;
        if (r.CurrentlyAvailableForWork.HasValue)
        {
            c.AvailabilityStatus = r.CurrentlyAvailableForWork.Value
                ? "Available"
                : "Not Available";
            c.AvailabilityUpdatedAt = DateTime.UtcNow;
        }
        c.NewsletterOptIn = r.NewsletterOptIn;
        c.UpdatedAt = DateTime.UtcNow;

        // email/mobile live on the User record
        if (c.User != null)
        {
            if (!string.IsNullOrWhiteSpace(r.Email)) c.User.Email = r.Email;
            if (!string.IsNullOrWhiteSpace(r.MobileNumber)) c.User.MobileNumber = r.MobileNumber;
            if (!string.IsNullOrWhiteSpace(r.CountryCode)) c.User.CountryCode = r.CountryCode;
        }

        var pct = await ComputeCompletionAsync(c);
        c.ProfileCompletionPct = pct;

        await _context.SaveChangesAsync();

        try
        {
            await _embeddingStorage.GenerateCandidateEmbeddingAsync(candidateId);
        }
        catch (Exception embedEx)
        {
            _logger.LogError(
       embedEx,
       "Failed to generate embedding for CandidateId: {CandidateId}",
       candidateId);

        }

        return new UpdateCandidatePersonalInfoResponseDto
        {
            Success = true,
            Message = "Personal info updated.",
            ProfileCompletionPct = pct
        };
    }

    // ============================================================
    // PROFILE PHOTO
    // ============================================================
    public async Task<UploadProfilePhotoResponseDto> UploadProfilePhotoAsync(
        Guid candidateId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return new() { Success = false, Message = "Please choose an image." };

        var c = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        // remove the previous photo if any
        if (!string.IsNullOrWhiteSpace(c.ProfilePhotoPublicId))
        {
            try { await _fileStorage.DeleteFileAsync(c.ProfilePhotoPublicId); }
            catch { /* ignore cleanup failure */ }
        }

        var upload = await _fileStorage.SaveFileAsync(photo, "profile-photos");

        c.ProfilePhotoUrl = upload.Url;
        c.ProfilePhotoPublicId = upload.PublicId;
        c.UpdatedAt = DateTime.UtcNow;
        c.ProfileCompletionPct = await ComputeCompletionAsync(c);

        await _context.SaveChangesAsync();

        return new UploadProfilePhotoResponseDto
        {
            Success = true,
            Message = "Profile photo updated.",
            ProfilePhotoUrl = upload.Url
        };
    }

    public async Task<UploadProfilePhotoResponseDto> DeleteProfilePhotoAsync(Guid candidateId)
    {
        var c = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        if (!string.IsNullOrWhiteSpace(c.ProfilePhotoPublicId))
        {
            try { await _fileStorage.DeleteFileAsync(c.ProfilePhotoPublicId); }
            catch { /* ignore */ }
        }

        c.ProfilePhotoUrl = null;
        c.ProfilePhotoPublicId = null;
        c.UpdatedAt = DateTime.UtcNow;
        c.ProfileCompletionPct = await ComputeCompletionAsync(c);
        await _context.SaveChangesAsync();

        return new UploadProfilePhotoResponseDto
        {
            Success = true,
            Message = "Profile photo removed.",
            ProfilePhotoUrl = null
        };
    }

    // ============================================================
    // COMPLETION
    // ============================================================
    public async Task<ProfileCompletionResponseDto> GetProfileCompletionAsync(Guid candidateId)
    {
        var c = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        var flags = await GetCompletionFlagsAsync(c);

        var pending = new List<string>();
        if (!flags.HasPhoto) pending.Add("Add a profile photo");
        if (!flags.HasPersonalInfo) pending.Add("Complete your personal information");
        if (!flags.HasSummary) pending.Add("Write a professional summary");
        if (!flags.HasResume) pending.Add("Upload your CV / resume");
        if (!flags.HasEducation) pending.Add("Add your education");
        if (!flags.HasWorkHistory) pending.Add("Add your work experience");
        if (!flags.HasSkills) pending.Add("Add your skills");
        if (!flags.HasAadhaar) pending.Add("Verify your Aadhaar");
        if (!flags.HasPassport) pending.Add("Add your passport (for international roles)");

        var pct = PctFromFlags(flags);

        // Itemised checklist so the UI can guide the candidate to 100%.
        // 9 sections, each weighted equally (~11% each).
        int w = (int)Math.Round(100.0 / 9.0);
        var items = new List<ProfileCompletionItemDto>
        {
            new() { Key = "photo",        Label = "Profile photo",         Completed = flags.HasPhoto,       WeightPct = w, ActionHint = "Add a profile photo",                 Endpoint = "POST /api/candidate/profile/profile-photo" },
            new() { Key = "personalInfo", Label = "Personal information",   Completed = flags.HasPersonalInfo,WeightPct = w, ActionHint = "Complete your personal information",     Endpoint = "PUT /api/candidate/profile/personal-info" },
            new() { Key = "summary",      Label = "Professional summary",   Completed = flags.HasSummary,     WeightPct = w, ActionHint = "Write a professional summary",          Endpoint = "PUT /api/candidate/profile/personal-info" },
            new() { Key = "resume",       Label = "CV / Resume",            Completed = flags.HasResume,      WeightPct = w, ActionHint = "Upload your CV / resume",               Endpoint = "POST /api/candidate/profile/documents/resume" },
            new() { Key = "education",    Label = "Education",              Completed = flags.HasEducation,   WeightPct = w, ActionHint = "Add your education",                    Endpoint = "POST /api/candidate/profile/education" },
            new() { Key = "workHistory",  Label = "Work experience",        Completed = flags.HasWorkHistory, WeightPct = w, ActionHint = "Add your work experience",              Endpoint = "POST /api/candidate/profile/work-experience" },
            new() { Key = "skills",       Label = "Skills",                 Completed = flags.HasSkills,      WeightPct = w, ActionHint = "Add your skills",                       Endpoint = "POST /api/candidate/profile/skills" },
            new() { Key = "aadhaar",      Label = "Aadhaar verification",   Completed = flags.HasAadhaar,     WeightPct = w, ActionHint = "Verify your Aadhaar",                    Endpoint = "POST /api/candidate/profile/documents" },
            new() { Key = "passport",     Label = "Passport",               Completed = flags.HasPassport,    WeightPct = w, ActionHint = "Add your passport (for international roles)", Endpoint = "POST /api/candidate/profile/documents" },
        };

        // keep the stored value in sync
        if (c.ProfileCompletionPct != pct)
        {
            c.ProfileCompletionPct = pct;
            c.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new ProfileCompletionResponseDto
        {
            Success = true,
            Message = "Profile completion loaded.",
            Data = new ProfileCompletionData
            {
                OverallPct = pct,
                HasPhoto = flags.HasPhoto,
                HasPersonalInfo = flags.HasPersonalInfo,
                HasSummary = flags.HasSummary,
                HasResume = flags.HasResume,
                HasEducation = flags.HasEducation,
                HasWorkHistory = flags.HasWorkHistory,
                HasSkills = flags.HasSkills,
                HasAadhaar = flags.HasAadhaar,
                HasPassport = flags.HasPassport,
                PendingActions = pending,
                RemainingPct = Math.Max(0, 100 - pct),
                Items = items
            }
        };
    }

    // ============================================================
    // CREATE PROFILE
    // ============================================================
    public async Task<CreateCandidateProfileResponseDto> CreateProfileAsync(
        Guid userId, CreateCandidateProfileRequestDto r)
    {
        var existing = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (existing != null)
            return new()
            {
                Success = false,
                Message = "A profile already exists for this user.",
                CandidateId = existing.CandidateId,
                ProfileCompletionPct = existing.ProfileCompletionPct
            };

        var c = new CandidateProfile
        {
            CandidateId = Guid.NewGuid(),
            UserId = userId,
            FullName = r.FullName ?? string.Empty,
            Role = r.Role,
            DateOfBirth = r.DateOfBirth,
            Gender = r.Gender,
            Nationality = r.Nationality,
            CurrentCity = r.CurrentCity,
            CurrentState = r.CurrentState,
            Pincode = r.Pincode,
            PreferredWorkLocation = r.PreferredWorkLocation,
            PreferredSalary = r.PreferredSalary,
            NoticePeriod = r.NoticePeriod,
            TotalExperienceYears = r.TotalExperienceYears,
            PrimaryTrade = r.PrimaryTrade,
            ProfessionalSummary = r.ProfessionalSummary,
            About = r.About,
            DisabilityStatus = r.DisabilityStatus,
            DisabilityNote = r.DisabilityNote,
            ItiCertified = r.ItiCertified,
            ItiTrade = r.ItiTrade,
            ItiMarks = r.ItiMarks,
            ItiCollege = r.ItiCollege,
            NewsletterOptIn = r.NewsletterOptIn,
            AvailabilityStatus =
                r.CurrentlyAvailableForWork.GetValueOrDefault(true)
                    ? "Available"
                    : "Not Available",
            AvailabilityUpdatedAt = DateTime.UtcNow,
            ProfileStatus = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        c.ProfileCompletionPct = await ComputeCompletionAsync(c, isNew: true);

        _context.CandidateProfiles.Add(c);
        await _context.SaveChangesAsync();

        return new CreateCandidateProfileResponseDto
        {
            Success = true,
            Message = "Profile created.",
            CandidateId = c.CandidateId,
            ProfileCompletionPct = c.ProfileCompletionPct
        };
    }

    // ============================================================
    // DISABILITY (PATCH)
    // ============================================================
    public async Task<UpdateDisabilityResponseDto> UpdateDisabilityAsync(
        Guid candidateId, UpdateDisabilityRequestDto r)
    {
        var c = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        c.DisabilityStatus = r.HasDisability;
        // clear the note when the candidate marks themselves as not disabled
        c.DisabilityNote = r.HasDisability ? r.DisabilityNote : null;
        c.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UpdateDisabilityResponseDto
        {
            Success = true,
            Message = "Disability details updated.",
            DisabilityStatus = c.DisabilityStatus,
            DisabilityNote = c.DisabilityNote
        };
    }

    public async Task<UpdateDisabilityResponseDto> GetDisabilityAsync(Guid candidateId)
    {
        var candidate = await _context.CandidateProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (candidate == null)
        {
            return new UpdateDisabilityResponseDto
            {
                Success = false,
                Message = "Profile not found."
            };
        }

        return new UpdateDisabilityResponseDto
        {
            Success = true,
            Message = "Disability details retrieved successfully.",
            DisabilityStatus = candidate.DisabilityStatus,
            DisabilityNote = candidate.DisabilityNote
        };
    }

    // ============================================================
    // AVAILABILITY FOR WORK (PATCH)
    // ============================================================
    public async Task<UpdateProfileAvailabilityResponseDto> UpdateAvailabilityAsync(
        Guid candidateId, UpdateProfileAvailabilityRequestDto r)
    {
        var c = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
        if (c == null)
            return new() { Success = false, Message = "Profile not found." };

        if (!string.IsNullOrWhiteSpace(r.AvailabilityStatus))
        {
            c.AvailabilityStatus = r.AvailabilityStatus.Trim();
        }
        else if (r.CurrentlyAvailableForWork.HasValue)
        {
            c.AvailabilityStatus = r.CurrentlyAvailableForWork.Value
                ? "Available"
                : "Not Available";
        }
        else
        {
            return new() { Success = false, Message = "No availability value supplied." };
        }

        c.AvailabilityUpdatedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();


        return new UpdateProfileAvailabilityResponseDto
        {
            Success = true,
            Message = "Availability updated.",
            AvailabilityStatus = c.AvailabilityStatus,
            AvailabilityUpdatedAt = c.AvailabilityUpdatedAt
        };
    }

    // ============================================================
    // Helpers
    // ============================================================
    private sealed class CompletionFlags
    {
        public bool HasPhoto, HasPersonalInfo, HasSummary, HasResume,
                    HasEducation, HasWorkHistory, HasSkills, HasAadhaar, HasPassport;
    }

    private async Task<CompletionFlags> GetCompletionFlagsAsync(CandidateProfile c, bool isNew = false)
    {
        var f = new CompletionFlags
        {
            HasPhoto = !string.IsNullOrWhiteSpace(c.ProfilePhotoUrl),
            HasPersonalInfo = !string.IsNullOrWhiteSpace(c.FullName) &&
                              (!string.IsNullOrWhiteSpace(c.CurrentCity) ||
                               !string.IsNullOrWhiteSpace(c.CurrentState)),
            HasSummary = !string.IsNullOrWhiteSpace(c.ProfessionalSummary) ||
                         !string.IsNullOrWhiteSpace(c.About)
        };

        if (!isNew)
        {
            f.HasResume = await _context.CandidateCvs.AnyAsync(x => x.CandidateId == c.CandidateId);
            f.HasEducation = await _context.CandidateEducations.AnyAsync(x => x.CandidateId == c.CandidateId);
            f.HasWorkHistory = await _context.CandidateWorkHistories.AnyAsync(x => x.CandidateId == c.CandidateId);
            f.HasSkills = await _context.CandidateSkills.AnyAsync(x => x.CandidateId == c.CandidateId);
            f.HasAadhaar = await _context.KycVerifications
                .AnyAsync(x => x.CandidateId == c.CandidateId && x.IdType == "Aadhaar");
            f.HasPassport = await _context.PassportVerifications
                .AnyAsync(x => x.CandidateId == c.CandidateId);
        }

        return f;
    }

    private static byte PctFromFlags(CompletionFlags f)
    {
        var checks = new[]
        {
            f.HasPhoto, f.HasPersonalInfo, f.HasSummary, f.HasResume,
            f.HasEducation, f.HasWorkHistory, f.HasSkills, f.HasAadhaar, f.HasPassport
        };
        var done = checks.Count(x => x);
        return (byte)Math.Round(done * 100.0 / checks.Length);
    }

    private async Task<byte> ComputeCompletionAsync(CandidateProfile c, bool isNew = false)
        => PctFromFlags(await GetCompletionFlagsAsync(c, isNew));
}