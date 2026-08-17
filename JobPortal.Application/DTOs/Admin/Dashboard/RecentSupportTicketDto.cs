using System;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Recent Support Tickets" table on Admin ▸ Dashboard.
    // GET /api/admin/dashboard/recent-support-tickets?limit=5
    public class RecentSupportTicketDto
    {
        public Guid TicketId { get; set; }

        public string RaisedByName { get; set; } = default!;

        public string Subject { get; set; } = default!;

        // Friendly label derived from SupportTicketType, e.g. "Billing",
        // "Technical", "Profile".
        public string Category { get; set; } = default!;

        // "Open" | "InProgress" | "Resolved"
        public string Status { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}