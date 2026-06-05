using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class UpdateCreditConfigurationRequestDto
    {
        [Range(1, 100)]
        public int ProfileUnlockCredits { get; set; }

        [Range(0, 100)]
        public int CvDownloadCredits { get; set; }

        [Range(1, 365)]
        public int CandidateAccessDays { get; set; }
    }
}
