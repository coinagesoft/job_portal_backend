using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class SkillsRequestDto
{
    /// <summary>
    /// List of required skills
    /// e.g. ["Java", "Spring Boot", "Safety Compliance"]
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one skill is required.")]
    public List<string> KeySkills { get; set; } = new();

    /// <summary>
    /// Additional job description — duties, responsibilities etc.
    /// </summary>
    public string? AdditionalJobDescription { get; set; }

    public string? LicenceDocsRequired { get; set; }

    public string? LanguageRequired { get; set; }
}