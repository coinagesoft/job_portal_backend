// ============================================================
//  JobPortal.Application/DTOs/Candidate/Profile/
//  CandidateProfileDtos.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Profile;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — PROFILE INFO
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight summary shown on the profile header card.
/// GET /api/candidate/profile/summary
/// </summary>
public class CandidateProfileSummaryResponseDto
{
    public bool   Success           { get; set; }
    public string Message           { get; set; } = string.Empty;
    public CandidateProfileSummaryData? Data { get; set; }
}

public class CandidateProfileSummaryData
{
    public Guid    CandidateId          { get; set; }
    public string  FullName             { get; set; } = string.Empty;
    public string? Role { get; set; }

    public string? ProfilePhotoUrl      { get; set; }
    public string? MobileNumber         { get; set; }
    public string? CountryCode          { get; set; }
    public string? Email                { get; set; }
    public string? CurrentCity          { get; set; }
    public string? CurrentState         { get; set; }
    public int     TotalExperienceYears { get; set; }
    public string? NoticePeriod         { get; set; }  // e.g. "30 Days", "Immediate"
    public string? About                { get; set; }  // short bio / headline
    public byte    ProfileCompletionPct { get; set; }
    public string  AvailabilityStatus   { get; set; } = "Available";
    public string? ProfessionalSummary { get; set; }


}

// ─────────────────────────────────────────────────────────────
// Personal Info — GET / PUT
// GET  /api/candidate/profile/personal-info
// PUT  /api/candidate/profile/personal-info
// ─────────────────────────────────────────────────────────────

public class CandidatePersonalInfoResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidatePersonalInfoData? Data { get; set; }
}

public class CandidatePersonalInfoData
{
    public Guid    CandidateId          { get; set; }
    public string  FullName             { get; set; } = string.Empty;
    public string  Role                  { get; set; } 
    public string? ProfilePhotoUrl      { get; set; }
    public DateOnly? DateOfBirth        { get; set; }
    public string? Gender               { get; set; }  // Male | Female | Prefer_Not_To_Say
    public string? Email                { get; set; }
    public string? MobileNumber         { get; set; }
    public string? CountryCode          { get; set; }
    public string? CurrentCity          { get; set; }
    public string? CurrentState         { get; set; }
    public string? Pincode              { get; set; }
    public string? ProfessionalSummary  { get; set; }  // longer bio
    public string? About                { get; set; }  // headline / short bio
    public string? NoticePeriod         { get; set; }
    public int     TotalExperienceYears { get; set; }
    public bool    NewsletterOptIn      { get; set; }
    public byte    ProfileCompletionPct { get; set; }
}

public class UpdateCandidatePersonalInfoRequestDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Multipart upload handled separately via /profile-photo endpoint.</summary>
    // Not included here intentionally

    public string? Role { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Gender { get; set; }   // Male | Female | Prefer_Not_To_Say

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }


    public string? MobileNumber { get; set; }

    public string? CountryCode { get; set; }


    [MaxLength(100)]
    public string? CurrentCity { get; set; }

    [MaxLength(100)]
    public string? CurrentState { get; set; }

    [MaxLength(10)]
    [RegularExpression(@"^\d{4,10}$", ErrorMessage = "Pincode must be 4–10 digits.")]
    public string? Pincode { get; set; }

    [MaxLength(2000)]
    public string? ProfessionalSummary { get; set; }

    [MaxLength(500)]
    public string? About { get; set; }

    /// <summary>e.g. "Immediate", "15 Days", "30 Days", "60 Days", "90 Days"</summary>
    [MaxLength(50)]
    public string? NoticePeriod { get; set; }

    [Range(0, 50)]
    public int TotalExperienceYears { get; set; }

    public bool NewsletterOptIn { get; set; }
}

public class UpdateCandidatePersonalInfoResponseDto
{
    public bool   Success           { get; set; }
    public string Message           { get; set; } = string.Empty;
    public byte   ProfileCompletionPct { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Profile Photo Upload
// POST /api/candidate/profile/profile-photo
// DELETE /api/candidate/profile/profile-photo
// ─────────────────────────────────────────────────────────────

public class UploadProfilePhotoResponseDto
{
    public bool   Success         { get; set; }
    public string Message         { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Profile Completion Breakdown
// GET /api/candidate/profile/completion
// ─────────────────────────────────────────────────────────────

public class ProfileCompletionResponseDto
{
    public bool   Success           { get; set; }
    public string Message           { get; set; } = string.Empty;
    public ProfileCompletionData? Data { get; set; }
}

public class ProfileCompletionData
{
    public byte   OverallPct        { get; set; }
    public bool   HasPhoto          { get; set; }
    public bool   HasPersonalInfo   { get; set; }
    public bool   HasSummary        { get; set; }
    public bool   HasResume         { get; set; }
    public bool   HasEducation      { get; set; }
    public bool   HasWorkHistory    { get; set; }
    public bool   HasSkills         { get; set; }
    public bool   HasAadhaar        { get; set; }
    public bool   HasPassport       { get; set; }

    /// <summary>Ordered list of next actions the candidate should take.</summary>
    public List<string> PendingActions { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────
// Enum Options (for dropdowns)
// GET /api/candidate/profile/enum-options
// ─────────────────────────────────────────────────────────────

public class CandidateProfileEnumOptionsDto
{
    public IEnumerable<string> GenderOptions      { get; set; } = Array.Empty<string>();
    public IEnumerable<string> NoticePeriodOptions { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AvailabilityOptions { get; set; } = Array.Empty<string>();
    public IEnumerable<string> DocumentTypes       { get; set; } = Array.Empty<string>();
}
