using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    /// <summary>
    /// AI match score breakdown returned on CV Search cards and Candidate Profile page.
    /// Populated only when a jobId is provided to the search or profile endpoint.
    /// </summary>
    public class AiScoreBreakdownDto
    {
        /// <summary>Weighted overall score 0–100.</summary>
        public int OverallScore { get; set; }

        /// <summary>Semantic embedding similarity (35% weight).</summary>
        public int AiSimilarityScore { get; set; }

        /// <summary>Skill overlap score (30% weight).</summary>
        public int SkillScore { get; set; }

        /// <summary>Trade / category match (20% weight).</summary>
        public int TradeScore { get; set; }

        /// <summary>Experience level match (10% weight).</summary>
        public int ExperienceScore { get; set; }

        /// <summary>Location proximity match (5% weight).</summary>
        public int LocationScore { get; set; }

        /// <summary>Excellent / Good / Fair / Low</summary>
        public string ScoreLabel { get; set; } = string.Empty;

        /// <summary>Human-readable match summary sentence.</summary>
        public string MatchReason { get; set; } = string.Empty;

        /// <summary>Job skills the candidate already has.</summary>
        public List<string> MatchedSkills { get; set; } = new();

        /// <summary>Job skills the candidate is missing.</summary>
        public List<string> MissingSkills { get; set; } = new();
    }
}