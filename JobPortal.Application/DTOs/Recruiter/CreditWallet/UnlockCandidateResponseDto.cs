using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class UnlockCandidateResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid UnlockId { get; set; }

        public Guid CandidateId { get; set; }

        public int CreditsDeducted { get; set; }

        public int RemainingCredits { get; set; }

        public DateTime AccessExpiresAt { get; set; }
    }
}
