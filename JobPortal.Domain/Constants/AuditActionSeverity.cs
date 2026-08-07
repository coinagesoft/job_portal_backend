using System.Collections.Generic;
using JobPortal.Domain.Enums;

namespace JobPortal.Domain.Constants
{
    // Single source of truth for "how severe is this audit action".
    // Every place that writes an AuditLog (AuthService, AdminUserService,
    // AdminSupportTicketService, ...) resolves its Severity from here
    // instead of hardcoding AuditSeverity.Warning/.Critical/.Info inline —
    // so the answer to "what severity should this log be" lives in exactly
    // one place in the backend, not scattered per call site where it can
    // drift or be set inconsistently.
    //
    // Add a new entry here whenever a new audited action is introduced;
    // anything not listed defaults to Info (see Resolve below) rather than
    // failing, since a missing mapping is far more likely to mean "this is
    // a routine/read action" than "this needs urgent attention".
    public static class AuditActionSeverity
    {
        private static readonly Dictionary<string, AuditSeverity> Map = new()
        {
            // Authentication
            ["Send OTP"] = AuditSeverity.Info,
            ["Resend OTP"] = AuditSeverity.Info,
            ["Login Success"] = AuditSeverity.Info,
            ["Login Failed"] = AuditSeverity.Warning,
            ["Logout"] = AuditSeverity.Info,

            // Sub Admin management
            ["Create Sub Admin"] = AuditSeverity.Warning,
            ["Update Sub Admin"] = AuditSeverity.Warning,
            ["Activate Sub Admin"] = AuditSeverity.Warning,
            ["Suspend Sub Admin"] = AuditSeverity.Critical,
            ["Delete Sub Admin"] = AuditSeverity.Critical,

            // Help & Support
            ["Reply to Ticket"] = AuditSeverity.Info,
        };

        /// <summary>
        /// Resolves the severity for a given audit Action name. Falls back
        /// to Info for any action not explicitly mapped above.
        /// </summary>
        public static AuditSeverity Resolve(string action)
        {
            return Map.TryGetValue(action, out var severity)
                ? severity
                : AuditSeverity.Info;
        }
    }
}