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

        // Sub-user allocation breakdown — RemainingCredits above is the
        // TOTAL still remaining across the owner and every sub-user
        // combined. These two split that total into "already handed out to
        // sub-users (and still theirs to spend)" vs "not yet allocated to
        // anyone, free for the owner to assign".
        public int AllocatedToSubUsers { get; set; }

        public int AvailableToAllocate { get; set; }
    }
}