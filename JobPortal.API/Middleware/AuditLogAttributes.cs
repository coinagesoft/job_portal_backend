using JobPortal.Domain.Enums;
using System;

namespace JobPortal.API.Middleware
{
    // Put this on any admin action that already writes its own, richer
    // AuditLog entry (e.g. Login, Create Sub Admin) so the global
    // AuditLogMiddleware doesn't create a duplicate row for it. Also
    // used on the audit-logs GET endpoint itself, and on any other
    // read-only endpoint that shouldn't show up in the log.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipAuditLogAttribute : Attribute
    {
    }

    // Optional per-action override so the automatic log entry reads
    // the way the UI expects (e.g. "User Suspended" instead of the raw
    // "SuspendUser" method name), and to set severity explicitly instead
    // of relying on the HTTP-method heuristic.
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditLogAttribute : Attribute
    {
        public string? Action { get; }
        public string? Module { get; }
        public AuditSeverity? Severity { get; }

        // Attribute constructors can only take the types the CLR can bake
        // into metadata as compile-time constants (bool, numeric types,
        // string, an enum type itself, Type, or object) — Nullable<T> is
        // NOT on that list, even though AuditSeverity? is a perfectly
        // normal property type. So the constructor takes a boxed
        // `object? severity` (an unboxed AuditSeverity value, or null when
        // omitted) and converts it to AuditSeverity? here, keeping the
        // nullable, easy-to-check Severity property for callers.
        public AuditLogAttribute(
            string? action = null,
            string? module = null,
            object? severity = null)
        {
            Action = action;
            Module = module;
            Severity = severity is AuditSeverity s ? s : null;
        }
    }
}