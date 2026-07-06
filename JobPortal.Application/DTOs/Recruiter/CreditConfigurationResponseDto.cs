using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class CreditConfigurationResponseDto
    {
        public Guid ConfigurationId { get; set; }

        public int ProfileUnlockCredits { get; set; }

        public int CvDownloadCredits { get; set; }

        public int CandidateAccessDays { get; set; }

        public bool IsActive { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Guid UpdatedBy { get; set; }
    }
}
