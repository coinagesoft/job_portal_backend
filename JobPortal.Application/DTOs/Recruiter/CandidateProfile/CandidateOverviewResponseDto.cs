using JobPortal.Application.DTOs.Recruiter.CVSearch;
using System;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateOverviewResponseDto
    {
        public Guid CandidateId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        public int TotalExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string AvailabilityStatus { get; set; } = string.Empty;

        public string? NoticePeriod { get; set; }

        public byte? AiMatchScore { get; set; }

        public bool IsUnlocked { get; set; }

        // ── AI Job Match (populated when jobId query param is provided) ──
        /// <summary>Title of the job this candidate was scored against.</summary>
        public string? AiMatchedJobTitle { get; set; }

        /// <summary>Detailed AI score breakdown vs a specific job posting.</summary>
        public AiScoreBreakdownDto? AiScoreBreakdown { get; set; }
    }
}