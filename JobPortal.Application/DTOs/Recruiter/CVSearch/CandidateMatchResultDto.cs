namespace JobPortal.Application.DTOs.Recruiter.CVSearch;

public class CandidateMatchResultDto
{
    public int MatchScore { get; set; }

    public string MatchReason { get; set; } = string.Empty;

    public int AiSimilarityScore { get; set; }

    public int SkillScore { get; set; }

    public int TradeScore { get; set; }

    public int ExperienceScore { get; set; }

    public int LocationScore { get; set; }
}