using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class DownloadCvResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid CandidateId { get; set; }

        public Guid CvId { get; set; }

        public string CvUrl { get; set; } = string.Empty;

        public int CreditsDeducted { get; set; }

        public int RemainingCredits { get; set; }
    }
}
