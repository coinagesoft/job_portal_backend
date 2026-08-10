namespace JobPortal.Domain.Enums;

/// <summary>
/// Who performed an audited action — used only for filtering
/// GET /api/admin/audit-logs?actorType=. A strongly-typed enum here
/// (rather than a free-text string) means Swagger renders a dropdown
/// of the only two valid values, and an invalid value is rejected by
/// model binding with an automatic 400 — it can no longer silently
/// fall through and match everything.
/// </summary>
public enum AuditActorType
{
    Admin = 0,
    SubAdmin = 1
}