using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Revenue & Credit Growth" stacked bar chart on
    // Admin ▸ Dashboard — monthly split by candidate memberships,
    // recruiter memberships and credit-plan purchases.
    // GET /api/admin/dashboard/revenue-credit-growth?months=6
    public class RevenueCreditGrowthResponseDto
    {
        // Month labels, oldest first, e.g. ["Jan", "Feb", ... ].
        public List<string> Labels { get; set; } = new();

        public List<decimal> CandidateMemberships { get; set; } = new();
        public List<decimal> RecruiterMemberships { get; set; } = new();
        public List<decimal> CreditPlans { get; set; } = new();
    }
}