namespace JobPortal.Application.DTOs.Candidate.Profile;

public class CreateCandidateProfileRequestDto
{
    // ==========================
    // Basic Information
    // ==========================

    public string? FullName { get; set; }

    public string? Role { get; set; }

    public string? Email { get; set; }

    public string? MobileNumber { get; set; }

    public string? CountryCode { get; set; }

    // ==========================
    // Personal Information
    // ==========================

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? CurrentCity { get; set; }

    public string? CurrentState { get; set; }

    public string? Pincode { get; set; }

    // ==========================
    // Career
    // ==========================

    public string? PreferredWorkLocation { get; set; }

    public int? PreferredSalary { get; set; }

    public string? NoticePeriod { get; set; }

    public int TotalExperienceYears { get; set; }

    public string? PrimaryTrade { get; set; }

    // ==========================
    // Profile
    // ==========================

    public string? ProfessionalSummary { get; set; }

    public string? About { get; set; }

    // ==========================
    // Disability
    // ==========================

    public bool DisabilityStatus { get; set; }

    public string? DisabilityNote { get; set; }

    // ==========================
    // ITI Details
    // ==========================

    public bool ItiCertified { get; set; }

    public string? ItiTrade { get; set; }

    public string? ItiMarks { get; set; }

    public string? ItiCollege { get; set; }

    // ==========================
    // Settings
    // ==========================

    public bool NewsletterOptIn { get; set; }
}