using System;

namespace JobPortal.Application.DTOs.Admin.Revenue
{
    // Powers the 4 summary cards at the top of Admin ▸ Revenue
    // (Total revenue / Candidate memberships / Recruiter memberships /
    // Recruiter credit plans).
    public class RevenueSummaryCardDto
    {
        public decimal Amount { get; set; }

        // % this category contributes to total revenue in the
        // filtered window. Null for the "Total revenue" card itself.
        public decimal? PercentOfTotal { get; set; }

        // % change vs the immediately preceding period of the same
        // length (e.g. this-month vs last-month when no explicit date
        // range is supplied). Null when there's no prior-period data
        // to compare against (e.g. a custom date range was supplied).
        public decimal? ChangePercentVsPrevious { get; set; }
    }

    public class RevenueSummaryDto
    {
        public RevenueSummaryCardDto TotalRevenue { get; set; } = new();
        public RevenueSummaryCardDto CandidateMemberships { get; set; } = new();
        public RevenueSummaryCardDto RecruiterMemberships { get; set; } = new();
        public RevenueSummaryCardDto CreditPlans { get; set; } = new();
    }
}