namespace JobPortal.Application.DTOs.Candidate.Profile;

public class CreateCandidateProfileRequestDto
{
    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? CurrentCity { get; set; }

    public string? CurrentState { get; set; }

    public string? Pincode { get; set; }

    public string? PreferredWorkLocation { get; set; }

    public int? PreferredSalary { get; set; }

    public string? NoticePeriod { get; set; }

    public string? About { get; set; }

    public string? ProfessionalSummary { get; set; }

    public bool DisabilityStatus { get; set; }

    public string? DisabilityNote { get; set; }

    public string? PrimaryTrade { get; set; }

    public int TotalExperienceYears { get; set; }

    public bool ItiCertified { get; set; }

    public string? ItiTrade { get; set; }

    public string? ItiMarks { get; set; }

    public string? ItiCollege { get; set; }

    public bool NewsletterOptIn { get; set; }
}