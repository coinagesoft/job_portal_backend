using System.ComponentModel.DataAnnotations;
using JobPortal.Domain.Enums.RecruiterEnums;
namespace JobPortal.Application.DTOs.JobPosting;

public class PublishingRequestDto
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    public DateOnly? ApplicationDeadline { get; set; }

    public CompanyVisibility? CompanyVisibility { get; set; }

    public JobType JobType { get; set; }
      = JobType.Normal_Job;
    /// <summary>
    /// Hot Job, Urgent Hiring, Premium Listing — optional tags
    /// </summary>
    public List<string>? PublishingTags { get; set; } = new();

    /// <summary>
    /// true = publish immediately
    /// false = save as draft
    /// </summary>
    [Required]
    public bool? PublishNow { get; set; } = true;
}

public class PublishingResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public string JobStatus { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public string? JobUrl { get; set; }
}