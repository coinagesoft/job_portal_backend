using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class SkillsRequestDto
{
    public List<string>? KeySkills { get; set; }

    public string? AdditionalJobDescription { get; set; }

    public string? LicenceDocsRequired { get; set; }

    public string? LanguageRequired { get; set; }
}