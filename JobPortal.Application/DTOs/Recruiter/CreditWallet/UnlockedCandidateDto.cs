using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{

    public class UnlockedCandidateDto
    {
        public Guid UnlockId { get; set; }

        public Guid CandidateId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string? Trade { get; set; }

        public int ExperienceYears { get; set; }

        public int CreditsDeducted { get; set; }

        public DateTime UnlockTimestamp { get; set; }

        public DateOnly UnlockExpiryDate { get; set; }

        public bool CvDownloadAllowed { get; set; }
    }
}
