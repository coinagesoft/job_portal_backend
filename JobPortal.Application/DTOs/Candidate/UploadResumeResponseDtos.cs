// ============================================================
//  JobPortal.Application/DTOs/Candidate/Profile/
//  UploadResumeResponseDto.cs  (UPDATED)
// ============================================================

namespace JobPortal.Application.DTOs.Candidate.Profile;

public class UploadResumeResponseDtos
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CvId { get; set; }
    public string? CvFileUrl { get; set; }
    public byte ProfileCompletionPct { get; set; }

    /// <summary>
    /// AI-parsed resume data returned immediately after upload.
    /// Frontend can use this to pre-fill profile form fields.
    /// null if Affinda parsing failed (upload still succeeds).
    /// </summary>
    public AiParsedResumeDto? AiParsed { get; set; }
}

public class AiParsedResumeDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Trade { get; set; }
    public int? ExperienceYrs { get; set; }
    public List<string> Skills { get; set; } = new();
    public decimal? ConfidenceScore { get; set; }
    public string? AffindaDocId { get; set; }

    // ── Newly surfaced fields ──────────────────────────────
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    public List<string> Languages { get; set; } = new();

    public List<AiParsedEducationDto> Education { get; set; } = new();

    public List<AiParsedWorkExperienceDto> WorkExperience { get; set; } = new();
}

public class AiParsedEducationDto
{
    public string? Qualification { get; set; }
    public string? Level { get; set; }
    public string? InstituteName { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
    public string? Grade { get; set; }
}

public class AiParsedWorkExperienceDto
{
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
}