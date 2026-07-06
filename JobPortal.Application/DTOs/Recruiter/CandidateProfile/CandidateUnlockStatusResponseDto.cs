using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateUnlockStatusResponseDto
    {
        public bool IsUnlocked { get; set; }

        public DateTime? UnlockDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public bool CvDownloadAllowed { get; set; }
    }
}
