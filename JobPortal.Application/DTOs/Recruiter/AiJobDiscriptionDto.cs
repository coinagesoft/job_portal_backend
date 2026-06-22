namespace JobPortal.Application.DTOs.Recruiter.AIJobDescription;

// ════════════════════════════════════════════════════════════
// AUTO-GENERATE  (triggered when employer fills all meta fields)
// ════════════════════════════════════════════════════════════

/// <summary>
/// Sent once the employer has filled: JobTitle, Role, TradeCategory,
/// ExperienceYears, JobType, EmploymentType.
/// The backend generates a full JD and returns it ready to drop into
/// the description textarea — no "click generate" button needed.
/// </summary>
public class AutoGenerateJdRequestDto
{
    public string JobTitle { get; set; } = string.Empty;
    public string? Role { get; set; }          // e.g. "Senior Welder"
    public string TradeCategory { get; set; } = string.Empty;
    public int ExperienceYears { get; set; } = 0;
    public string? JobType { get; set; }          // "Normal_Job" | "Hot_Vacancy" | "Classified"
    public string? EmploymentType { get; set; }          // "Permanent" | "Contract" | …
}

public class AutoGenerateJdResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Single ready-to-use job description text.
    /// Drop this straight into the JD textarea on the frontend.
    /// </summary>
    public string GeneratedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Skill chips to pre-populate the Step-3 skills field.
    /// </summary>
    public List<string> SuggestedSkills { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// INLINE SUGGESTIONS  (triggered while employer is typing)
// ════════════════════════════════════════════════════════════

/// <summary>
/// Sent on debounced keyup (e.g. every 600 ms) while the employer
/// is typing in the Job Description textarea.
/// Returns short continuation / improvement suggestions.
/// </summary>
public class JdInlineSuggestionRequestDto
{
    // Context fields — whatever the employer has filled so far
    public string? JobTitle { get; set; }
    public string? Role { get; set; }
    public string? TradeCategory { get; set; }
    public int? ExperienceYears { get; set; }
    public string? JobType { get; set; }

    /// <summary>The current text the employer has typed so far.</summary>
    public string CurrentText { get; set; } = string.Empty;
}

public class JdInlineSuggestionResponseDto
{
    public bool Success { get; set; }
    /// <summary>
    /// 3 short continuation / improvement suggestions.
    /// Frontend shows these as ghost-text or a dropdown the employer can click.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// SKILL SUGGESTIONS  (Step 3 helper — kept from before)
// ════════════════════════════════════════════════════════════

public class AiSkillSuggestionRequestDto
{
    public string JobTitle { get; set; } = string.Empty;
    public string TradeCategory { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
}

public class AiSkillSuggestionResponseDto
{
    public bool Success { get; set; }
    public List<string> Skills { get; set; } = new();
}
