using System;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Platform Overview" panel on Admin ▸ Dashboard — one
    // glance at Plans, Users, Audit Logs and Legal Pages.
    // GET /api/admin/dashboard/platform-overview
    public class PlatformOverviewResponseDto
    {
        public PlansOverviewDto Plans { get; set; } = new();
        public UsersOverviewDto Users { get; set; } = new();
        public AuditOverviewDto AuditLogs { get; set; } = new();
        public LegalPagesOverviewDto LegalPages { get; set; } = new();
    }

    public class PlansOverviewDto
    {
        public int ActiveCount { get; set; }
        public int RecruiterPlanCount { get; set; }
        public int CandidatePlanCount { get; set; }
        public int CreditPlanCount { get; set; }
    }

    public class UsersOverviewDto
    {
        // Admin / sub-admin accounts (Admin ▸ Users), not candidates/recruiters.
        public int Total { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
    }

    public class AuditOverviewDto
    {
        public int CriticalLast24Hours { get; set; }
        public int TotalLast24Hours { get; set; }
    }

    public class LegalPagesOverviewDto
    {
        public int TotalDocuments { get; set; }
        public int PublishedCount { get; set; }
        public DateTime? LastPublishedAt { get; set; }
    }
}