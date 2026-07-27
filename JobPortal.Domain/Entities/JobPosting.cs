using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;

namespace JobPortal.Domain.Entities;

public class JobPosting
{
    public Guid JobId { get; set; }
    public Guid EmployerId { get; set; }
    public Guid? PostedBySubUserId { get; set; }
    public string JobTitle { get; set; } = default!;
    public string JobDescription { get; set; } = default!;
    public string? Role { get; set; }
    public string TradeCategory { get; set; } = default!;
    public int SalaryMin { get; set; }
    public int SalaryMax { get; set; }

    public string? ContractPeriod { get; set; }

    // ✅ ALL string — no enums on entity
    public string SalaryCurrency { get; set; } 
    public string SalaryDisplayOption { get; set; } = default;
    public short Vacancies { get; set; } = 0;
    public byte ExperienceMinYears { get; set; } = 0; 
    public byte ExperienceMaxYears { get; set; } = 0;
    public byte? AgeMin { get; set; }
    public byte? AgeMax { get; set; }
    public GenderPreferred GenderPreferred { get; set; } = GenderPreferred.Any;
    public string? EducationRequired { get; set; }
    public string? LicenceDocsRequired { get; set; }
    public string? LanguageRequired { get; set; }
    public List<string>? KeySkills { get; set; }
    public bool DisabilityEligible { get; set; } = false;
    public LocationType LocationType { get; set; } = LocationType.Onshore;
    // Onshore
    public string? WorkAddressLine { get; set; }
    public string? OnshoreCity { get; set; }
    public string? OnshoreState { get; set; }
    public string? OnshoreCountry { get; set; }
    public string? OnshorePincode { get; set; }

    // Offshore
    public string? OffshoreVesselName { get; set; }
    public string? OffshoreRegion { get; set; }
    public string? OffshoreCountry { get; set; }
    public bool IsInternational { get; set; } = false;
    public bool PassportRequired { get; set; } = false;
    public byte? PassportValidityMonths { get; set; }
    public CompanyVisibility CompanyVisibility { get; set; } = CompanyVisibility.ShowName;
    public DateOnly ApplicationDeadline { get; set; }
    public int AppliedCount { get; set; } = 0;
    public JobStatus JobStatus { get; set; } = JobStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CurrentStep { get; set; } = 0;
    public int LastCompletedStep { get; set; } = 0;
    public List<string>? ScreeningQuestions { get; set; }
    public List<string>? PublishingTags { get; set; }
    public string JobType { get; set; } = default;

    //new Fields
    // Employment
    public byte? DutyHoursPerDay { get; set; }
    public bool PaidOvertime { get; set; }
    public List<string>? KeyResponsibilities { get; set; }
    public string EmploymentType { get; set; } = default;
    public string EmploymentMode { get; set; } = default;
    // Department shown on card
    public string? Department { get; set; }
    // NEW
    public string IndustryType { get; set; } = default!;
    // Analytics
    public int ViewCount { get; set; } = 0;

    // Benefits
    public List<string>? Benefits { get; set; }

    public bool IsFeatured { get; set; } = false;
    public bool IsUrgentHiring { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    // Active/Inactive
    public bool IsActive { get; set; } = true;

    // Company analytics

    // Skills tags
    public List<string>? Tags { get; set; }

    // SEO/Search
    public string? SearchKeywords { get; set; }

    // Navigation
    public EmployerProfile EmployerProfile { get; set; } = default!;
    public EmployerSubUser? PostedBySubUser { get; set; }
    public ICollection<JobApplication> Applications { get; set; }
        = new List<JobApplication>();
}

