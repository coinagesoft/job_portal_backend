using JobPortal.Application.DTOs.Recruiter;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.JobPosting;

public class JobDetailsRequestDto
{
    /// <summary>
    /// e.g. Welder 6G, Senior Electrician
    /// </summary>
    /// 
    public Guid? JobId { get; set; }

    [Required(ErrorMessage = "Job title is required.")]
    [MaxLength(200)]
    public string? JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Role searched or typed by user — e.g. Welder, Fitter, Driver
    /// Not a fixed dropdown — free search
    /// </summary>
    [Required(ErrorMessage = "Trade category is required.")]
    [MaxLength(100)]
    public string? TradeCategory { get; set; } = string.Empty;

    /// <summary>
    /// Specific role within trade — optional
    /// e.g. "Senior Welder", "Pipe Fitter"
    /// </summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    [Required]
    public int? ExperienceRequiredYears { get; set; } = 0;

    [Required]
    public JobType? JobType { get; set; }

    [Required]
    public EmploymentType? EmploymentType { get; set; }

    [Required(ErrorMessage = "Job description is required.")]
    [MinLength(50, ErrorMessage = "Description must be at least 50 characters.")]
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
