namespace JobPortal.Domain.Enums;

/// <summary>
/// Severity of an admin-panel audit log entry, shown as a colored badge
/// on /admin/audit (Info = green, Warning = amber, Critical = red).
/// </summary>
public enum AuditSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}