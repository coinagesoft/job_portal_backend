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

    // ✅ ALL string — no enums on entity
    public string SalaryCurrency { get; set; } = "INR";
    public string SalaryDisplayOption { get; set; } = "Show_Range";
    public short Vacancies { get; set; } = 1;
    public byte ExperienceRequiredYears { get; set; } = 0;
    public byte? AgeMin { get; set; }
    public byte? AgeMax { get; set; }
    public string GenderPreferred { get; set; } = "Any";
    public string? EducationRequired { get; set; }
    public string? LicenceDocsRequired { get; set; }
    public string? LanguageRequired { get; set; }
    public string? KeySkills { get; set; }
    public bool DisabilityEligible { get; set; } = false;
    public string LocationType { get; set; } = "Onshore";
    public string? OnshoreCity { get; set; }
    public string? OnshoreState { get; set; }
    public string? OffshoreVesselName { get; set; }
    public string? OffshoreRegion { get; set; }
    public bool IsInternational { get; set; } = false;
    public bool PassportRequired { get; set; } = false;
    public byte? PassportValidityMonths { get; set; }
    public string CompanyVisibility { get; set; } = "Show_Name";
    public DateOnly ApplicationDeadline { get; set; }
    public int AppliedCount { get; set; } = 0;
    public string JobStatus { get; set; } = "Draft";
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CurrentStep { get; set; } = 0;
    public int LastCompletedStep { get; set; } = 0;
    public string? ScreeningQuestions { get; set; }
    public string? PublishingTags { get; set; }

    // Navigation
    public EmployerProfile EmployerProfile { get; set; } = default!;
    public EmployerSubUser? PostedBySubUser { get; set; }
    public ICollection<JobApplication> Applications { get; set; }
        = new List<JobApplication>();
}