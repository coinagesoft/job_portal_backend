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

        // Set to "MEMBERSHIP_EXPIRED" when the unlock was refused because the
        // recruiter's membership validity has ended, so the app can send them
        // to the membership plans page instead of just showing an error.
        public string? Code { get; set; }

        public bool MembershipExpired { get; set; }

        public Guid UnlockId { get; set; }

        public Guid CandidateId { get; set; }

        public int CreditsDeducted { get; set; }

        public int RemainingCredits { get; set; }

        public DateTime AccessExpiresAt { get; set; }
    }
}