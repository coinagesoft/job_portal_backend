using System;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Stats widgets" section at the top of Admin ▸ Dashboard
    // (https://.../admin/dashboard) — the 4 primary cards (Total Revenue,
    // Total Candidates, Total Recruiters, Credits Sold) plus the 3
    // secondary cards (Active Job Postings, Pending Verifications,
    // Open Support Tickets).
    // GET /api/admin/dashboard/stats-widgets
    public class StatsWidgetsResponseDto
    {
        public StatCardDto TotalRevenue { get; set; } = new();
        public StatCardDto TotalCandidates { get; set; } = new();
        public StatCardDto TotalRecruiters { get; set; } = new();
        public StatCardDto CreditsSold { get; set; } = new();

        public JobPostingsStatDto ActiveJobPostings { get; set; } = new();
        public PendingVerificationsStatDto PendingVerifications { get; set; } = new();
        public SupportTicketsStatDto OpenSupportTickets { get; set; } = new();
    }

    // Generic "value" card, used by every primary stat on the dashboard.
    public class StatCardDto
    {
        public decimal Value { get; set; }
    }

    public class JobPostingsStatDto
    {
        public int Active { get; set; }
        public int Paused { get; set; }
    }

    public class PendingVerificationsStatDto
    {
        public int Total { get; set; }

        // Pending for more than 7 days — surfaced as "High Priority"
        // on the card, same idea as the badge on /admin/verifications.
        public int HighPriority { get; set; }
    }

    public class SupportTicketsStatDto
    {
        // Every ticket not yet Resolved.
        public int Open { get; set; }

        // Subset of Open that hasn't been picked up yet (Status == "Open",
        // i.e. not "InProgress").
        public int Pending { get; set; }
    }
}