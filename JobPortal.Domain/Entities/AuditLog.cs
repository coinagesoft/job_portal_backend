using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobPortal.Domain.Enums;

namespace JobPortal.Domain.Entities;


public class AuditLog
{
    public Guid LogId { get; set; }

    public Guid PerformedByAdminId { get; set; }

    public string PerformedByName { get; set; } = default!;

    public string PerformedByRole { get; set; } = default!;

    // Recruiters, Candidates, Jobs, Plans...
    public string Module { get; set; } = default!;

    // Create, Update, Delete, Approve...
    public string Action { get; set; } = default!;

    public string? TargetEntityType { get; set; }

    public Guid? TargetEntityId { get; set; }

    // Human-readable label for the "Target Entity" column, e.g. "John Doe",
    // "Q3 Revenue Report". Falls back to TargetEntityType when not set.
    public string? TargetEntityName { get; set; }

    // Info | Warning | Critical — drives the severity badge on /admin/audit
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    // JSON
    public string? OldValues { get; set; }

    // JSON
    public string? NewValues { get; set; }

    public string? Description { get; set; }

    public string IpAddress { get; set; } = default!;

    public string? UserAgent { get; set; }

    public bool Success { get; set; } = true;

    // The admin session (see AdminSession) that was active when this
    // action was performed. Resolved from the "jti" claim on the
    // request's JWT. Null for actions that write their own AuditLog
    // row outside of a normal authenticated admin request.
    public Guid? SessionId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public AdminUser PerformedByAdmin { get; set; } = default!;

    public AdminSession? Session { get; set; }
}