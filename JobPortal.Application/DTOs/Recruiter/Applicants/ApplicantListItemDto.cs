using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class ApplicantListItemDto
    {
        public Guid ApplicationId { get; set; }

        public Guid CandidateId { get; set; }

        public Guid JobId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public string? PrimaryTrade { get; set; }

        public int ExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string ApplicationStatus { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; }

        public bool IsShortlisted { get; set; }

        public bool IsUnlocked { get; set; }

        public bool CvDownloaded { get; set; }
    }
}
