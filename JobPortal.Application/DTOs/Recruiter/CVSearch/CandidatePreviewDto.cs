using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CandidatePreviewDto
    {
        public Guid CandidateId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        public int ExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string? ProfessionalSummary { get; set; }

        public string AvailabilityStatus { get; set; } = string.Empty;

        public bool IsItiCertified { get; set; }

        public bool IsKycVerified { get; set; }

        public bool IsPassportValid { get; set; }

        public List<string> Skills { get; set; } = new();
    }
}
