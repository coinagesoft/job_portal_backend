using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{

    public class WalletSummaryDto
    {
        public Guid EmployerId { get; set; }

        public int CreditBalance { get; set; }

        public int AllocatedCredits { get; set; }

        public int AvailableCredits { get; set; }

        public string? PackageName { get; set; }

        public DateTime? PackExpiresAt { get; set; }

        // True once the membership has ended. CreditBalance/AvailableCredits
        // are 0 then; the unusable leftover is reported in ExpiredCredits.
        public bool MembershipExpired { get; set; }

        public int ExpiredCredits { get; set; }
    }
}