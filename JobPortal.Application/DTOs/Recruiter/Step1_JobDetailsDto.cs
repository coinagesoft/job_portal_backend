using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.JobPosting;

public class JobDetailsRequestDto
{
    public Guid? JobId { get; set; }

    [Required(ErrorMessage = "Job title is required.")]
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [Required(ErrorMessage = "Trade category is required.")]
    [MaxLength(100)]
    public string? TradeCategory { get; set; }

    [MaxLength(100)]
    public string? Role { get; set; }

    [Range(0, 50)]
    public int? ExperienceMinYears { get; set; }

    [Range(0, 50)]
    public int? ExperienceMaxYears { get; set; }

    [Required]
    public string JobType { get; set; } = "Regular Hiring";

    public string IndustryType { get; set; } = default!;


    [Required]
    public string EmploymentType { get; set; } = string.Empty;

    [Required]
    public string EmploymentMode { get; set; } = string.Empty;

    public string? Department { get; set; }

    public int? DutyHoursPerDay { get; set; }
    public bool IsClientHiring { get; set; }

    public string? ClientName { get; set; }

    public bool ShowClientName { get; set; }
    public bool? PaidOvertime { get; set; }

    public List<string>? KeyResponsibilities { get; set; } = new();

    [Required(ErrorMessage = "Job description is required.")]
    public string? JobDescription { get; set; } = string.Empty;
}
public class JobDetailsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }         // created here, used in all next steps
    public string JobStatus { get; set; } = "Draft";
    public JobStepStatusDto? StepStatus { get; set; }
    public bool IsClientHiring { get; set; }

    public string? ClientName { get; set; }

    public bool ShowClientName { get; set; }
}