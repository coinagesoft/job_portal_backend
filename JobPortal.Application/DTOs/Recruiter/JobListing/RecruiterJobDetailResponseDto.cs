using JobPortal.Application.DTOs.JobPosting;
using JobPortal.Domain.Enums.RecruiterEnums;

namespace JobPortal.Application.DTOs.Recruiter.JobListing;

public class RecruiterJobDetailResponseDto
{
    // =====================================================
    // Basic
    // =====================================================

    public Guid JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public string TradeCategory { get; set; } = string.Empty;
    
    public string? Role { get; set; }

    public bool? IsOilField { get; set; }

    public string? Department { get; set; }

    // =====================================================
    // Job Type
    // =====================================================

    public string JobType { get; set; }

    public string EmploymentType { get; set; }

    public string EmploymentMode { get; set; }

    public string JobStatus { get; set; } = string.Empty;

    // =====================================================
    // Salary
    // =====================================================

    public int SalaryMin { get; set; }

    public int SalaryMax { get; set; }

    public SalaryCurrency SalaryCurrency { get; set; }

    public string SalaryDisplayOption { get; set; }

    // =====================================================
    // Vacancies & Experience
    // =====================================================

    public short Vacancies { get; set; }

    public byte ExperienceMinYears { get; set; }

    public byte ExperienceMaxYears { get; set; }

    public byte? DutyHoursPerDay { get; set; }

    public bool PaidOvertime { get; set; }

    // =====================================================
    // Eligibility
    // =====================================================

    public string? EducationRequired { get; set; }

    public GenderPreferred GenderPreferred { get; set; }

    public byte? AgeMin { get; set; }

    public byte? AgeMax { get; set; }

    public bool DisabilityEligible { get; set; }

    public bool PassportRequired { get; set; }

    public byte? PassportValidityMonths { get; set; }

    // =====================================================
    // Skills
    // =====================================================

    public List<string> KeySkills { get; set; } = new();

    public List<string> KeyResponsibilities { get; set; } = new();

    public List<string> Benefits { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public string? LanguageRequired { get; set; }

    public string? LicenceDocsRequired { get; set; }

    // =====================================================
    // Location
    // =====================================================

    public LocationType LocationType { get; set; }

    public string? WorkAddressLine { get; set; }

    public string? OnshoreCity { get; set; }

    public string? OnshoreState { get; set; }

    public string? OnshoreCountry { get; set; }

    public string? OnshorePincode { get; set; }

    public string? OffshoreVesselName { get; set; }

    public string? OffshoreRegion { get; set; }

    public string? OffshoreCountry { get; set; }

    public bool IsInternational { get; set; }

    // =====================================================
    // Publishing
    // =====================================================

    public CompanyVisibility CompanyVisibility { get; set; }

    public DateOnly ApplicationDeadline { get; set; }

    public List<ScreeningQuestion> ScreeningQuestions { get; set; } = new();

    public List<string> PublishingTags { get; set; } = new();

    // =====================================================
    // Analytics
    // =====================================================

    public int AppliedCount { get; set; }

    public int ViewCount { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsUrgentHiring { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    // =====================================================
    // Audit
    // =====================================================

    public int CurrentStep { get; set; }

    public int LastCompletedStep { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }
}