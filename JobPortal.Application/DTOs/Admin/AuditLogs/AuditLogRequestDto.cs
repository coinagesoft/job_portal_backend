using System;

namespace JobPortal.Application.DTOs.Admin.AuditLogs
{
    // Backs the filter bar on /admin/audit:
    // GET /api/admin/audit-logs?action=&date=&actorType=&severity=&page=&pageSize=
    public class AuditLogRequestDto
    {
        // Free-text search over the Action column (e.g. "User Suspended").
        public string? Action { get; set; }

        // Filters to logs created on this calendar day (local to the date picker).
        public DateTime? Date { get; set; }

        // "Admin" | "Sub-Admin"
        public string? ActorType { get; set; }

        // "Info" | "Warning" | "Critical"
        public string? Severity { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}