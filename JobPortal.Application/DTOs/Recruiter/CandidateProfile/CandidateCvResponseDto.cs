using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateCvResponseDto
    {
        public Guid CvId { get; set; }

        public string? ParsedTrade { get; set; }

        public int? ParsedExperienceYrs { get; set; }

        public decimal? AiConfidenceScore { get; set; }

        public DateTime? GeneratedAt { get; set; }

        public bool CvAvailable { get; set; }

        public bool CanDownloadCv { get; set; }
    }
}
