using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class ApplyJobDetailsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid JobId { get; set; }

        // Company

        public string? CompanyName { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public bool IsConfidentialCompany { get; set; }

        // Job

        public string JobTitle { get; set; } = string.Empty;

        public string EmploymentType { get; set; } = string.Empty;

        public string? Department { get; set; }

        public string? JobTradeCategory { get; set; }

        public string Location { get; set; } = string.Empty;

        // Requirements

        public byte? AgeMin { get; set; }

        public byte? AgeMax { get; set; }

        public string GenderPreferred { get; set; } = "Any";

        public bool DisabilityEligible { get; set; }

        /// <summary>
        /// The candidate's own trade category (from their profile), echoed
        /// back so the UI can show it in the block message.
        /// </summary>
        public string? CandidateTradeCategory { get; set; }

        /// <summary>
        /// True when the candidate has a trade set on their profile and it
        /// doesn't match this job's TradeCategory. The candidate cannot
        /// apply while this is true — the frontend should block the Apply
        /// action and show a toast explaining why.
        /// </summary>
        public bool TradeCategoryMismatch { get; set; }

        public bool PassportRequired { get; set; }

        /// <summary>
        /// True when the logged-in candidate has a passport record on file
        /// (JobPortal.Domain.Entities.PassportVerification). Used by the
        /// frontend to block "Apply" (or show a clear "You don't have a
        /// passport" message) instead of relying on a self-attested
        /// checkbox when PassportRequired is true.
        /// </summary>
        public bool CandidateHasPassport { get; set; }

        public List<string> LanguagesRequired { get; set; } = new();

        public List<string> PersonalDocumentsRequired { get; set; } = new();

        public List<string> WorkingDocumentsRequired { get; set; } = new();

        public List<string> ScreeningQuestions { get; set; } = new();

        // Candidate

        public bool HasUploadedCv { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string? CandidatePhotoUrl { get; set; }
    }
}