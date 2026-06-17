using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class CreditWalletDashboardDto
    {
        // Top Card
        public int RemainingCredits { get; set; }

        public string? PlanName { get; set; }

        public DateTime? PlanExpiryDate { get; set; }

        // Widget 1
        public int CreditsUsedThisMonth { get; set; }

        // Widget 2
        public int ProfilesUnlocked { get; set; }

        // Widget 3
        public bool SharedWalletEnabled { get; set; }

        public int TotalSubUsers { get; set; }
    }
}
