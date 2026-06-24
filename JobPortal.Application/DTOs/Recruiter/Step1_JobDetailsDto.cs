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

    // Experience

    [Range(0, 50)]
    public int? ExperienceMinYears { get; set; }

    [Range(0, 50)]
    public int? ExperienceMaxYears { get; set; }

    // Employment

    [Required]
    public JobType? JobType { get; set; }

    [Required]
    public EmploymentType EmploymentType { get; set; }

    [Required]
    public EmploymentMode EmploymentMode { get; set; }

    // Department

    [MaxLength(100)]
    public string? Department { get; set; }

    // Work schedule

    [Range(1, 24)]
    public int? DutyHoursPerDay { get; set; }

    public bool? PaidOvertime { get; set; }

    // Responsibilities

    public List<string>? KeyResponsibilities { get; set; } = new();

    // Description

    [Required(ErrorMessage = "Job description is required.")]
    [MinLength(50,
        ErrorMessage = "Description must be at least 50 characters.")]
    public string? JobDescription { get; set; } = string.Empty;
}

public class JobDetailsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }         // created here, used in all next steps
    public string JobStatus { get; set; } = "Draft";
    public JobStepStatusDto? StepStatus { get; set; }
}
