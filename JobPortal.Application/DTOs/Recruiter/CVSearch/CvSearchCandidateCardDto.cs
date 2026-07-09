using System;
using System.Collections.Generic;
using JobPortal.Application.DTOs.Recruiter.CandidateProfile;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CvSearchCandidateCardDto
    {
        public Guid CandidateId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        /// <summary>
        /// The candidate's self-entered "Trade / Job Title" from their Personal
        /// tab. PrimaryTrade is often null for older profiles (it's only set at
        /// registration or via resume parsing), so the frontend should prefer
        /// Role and fall back to PrimaryTrade.
        /// </summary>
        public string? Role { get; set; }

        public int ExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string AvailabilityStatus { get; set; } = string.Empty;

        public int KeywordMatchPercentage { get; set; }

        public string? Band { get; set; }

        public bool IsItiCertified { get; set; }

        public bool IsKycVerified { get; set; }

        public bool IsPassportValid { get; set; }

        public bool IsUnlocked { get; set; }

        public bool CanDownloadCv { get; set; }

        public int UnlockCredits { get; set; }

        public int MatchScore { get; set; }

        public int AiMatchScore { get; set; }

        public string? MatchReason { get; set; }

        // ── AI Score Breakdown (populated when JobId is provided) ──
        /// <summary>Full AI score breakdown. Null when no jobId was supplied.</summary>
        public AiScoreBreakdownDto? AiScoreBreakdown { get; set; }

        /// <summary>The job title this AI score is calculated against.</summary>
        public string? AiMatchedJobTitle { get; set; }

        public List<string> Skills { get; set; } = new();
    }
}