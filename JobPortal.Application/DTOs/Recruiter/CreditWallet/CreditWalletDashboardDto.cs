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

        // Credits left over after the membership ended. Unusable, and
        // forfeited when the membership is renewed.
        public int ExpiredCredits { get; set; }

        public string? PlanName { get; set; }

        public DateTime? PlanExpiryDate { get; set; }

        // Membership state. When MembershipExpired is true the recruiter must
        // renew the membership before buying more credits.
        public bool IsMember { get; set; }

        public DateTime? MembershipExpiresAt { get; set; }

        public bool MembershipExpired { get; set; }

        // Member with no end date (a plan without a validity period): there is
        // nothing to expire or renew.
        public bool IsLifetimeMembership { get; set; }

        // True while the membership is still running but inside the optional
        // early-renewal window (Membership:EarlyRenewalWindowDays), so the
        // recruiter may renew now. The new term then starts when the current
        // one ends and unused credits are carried over.
        public bool CanRenewEarly { get; set; }

        public int EarlyRenewalWindowDays { get; set; }

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