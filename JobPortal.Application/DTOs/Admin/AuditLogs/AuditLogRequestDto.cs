using System;
using JobPortal.Domain.Enums;

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

        // Admin | SubAdmin. Typed as an enum (not a free-text string) so
        // Swagger shows only the two valid values and an invalid value is
        // rejected by model binding with an automatic 400 — it can no
        // longer silently fall through and match every row.
        public AuditActorType? ActorType { get; set; }

        // Info | Warning | Critical. Same reasoning as ActorType above —
        // typed as an enum so an invalid value is rejected instead of
        // silently being treated as "no filter".
        public AuditSeverity? Severity { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}