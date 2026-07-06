using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class SkillsRequestDto
{
    // Main skills shown on job card and used in AI matching
    public List<string>? KeySkills { get; set; } = new();

    
    public string? LicenceDocsRequired { get; set; }

    // Example:
    // "English"
    // "English, Hindi"
    public string? LanguageRequired { get; set; }

    // Benefits section
    public List<string>? Benefits { get; set; } = new();

    // Search tags
    public List<string>? Tags { get; set; } = new();
}