namespace JobPortal.Application.DTOs.Recruiter.CVSearch;

// ── Request ──────────────────────────────────────────────────

// ── Response ─────────────────────────────────────────────────
public class RankedCandidateListDto
{
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public int TotalCandidatesEvaluated { get; set; }
    public List<RankedCandidateDto> Candidates { get; set; } = new();
}

public class RankedCandidateDto
{
    public int Rank { get; set; }
    public Guid CandidateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PrimaryTrade { get; set; }
    public int TotalExperienceYears { get; set; }
    public string? CurrentCity { get; set; }
    public string? Band { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    // ── Score Breakdown ──────────────────────────────────────
    public ScoreBreakdownDto ScoreBreakdown { get; set; } = new();
}

public class ScoreBreakdownDto
{
    /// <summary>Overall weighted match score (0–100)</summary>
    public int OverallScore { get; set; }

    /// <summary>AI semantic similarity score (35% weight)</summary>
    public int AiSimilarityScore { get; set; }

    /// <summary>Skill overlap score (30% weight)</summary>
    public int SkillScore { get; set; }

    /// <summary>Trade/category match score (20% weight)</summary>
    public int TradeScore { get; set; }

    /// <summary>Experience match score (10% weight)</summary>
    public int ExperienceScore { get; set; }

    /// <summary>Location match score (5% weight)</summary>
    public int LocationScore { get; set; }

    /// <summary>Human-readable match summary</summary>
    public string MatchReason { get; set; } = string.Empty;

    /// <summary>Individual score label: Excellent / Good / Fair / Low</summary>
    public string ScoreLabel { get; set; } = string.Empty;
}

// ── Single Candidate Score (used by profile page) ───────────
public class CandidateProfileScoreResponseDto
{
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public ScoreBreakdownDto ScoreBreakdown { get; set; } = new();
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
}
