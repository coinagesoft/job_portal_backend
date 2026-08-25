using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Infrastructure.Extensions
{
    /// <summary>
    /// Every admin audit-log row is supposed to carry the AdminSession
    /// (see AdminSession.SessionId) that was active when the action was
    /// performed, so a support/security investigation can pivot from a
    /// single suspicious log entry to "everything else this login did".
    ///
    /// The access token only carries the "jti" claim (AdminSession.JwtId),
    /// so every write site — the generic AuditLogMiddleware as well as the
    /// handful of services that write their own, richer AuditLog rows
    /// (sub-admin management, recruiter/candidate status changes, document
    /// verification, support replies) — needs to resolve jti -> SessionId
    /// the same way. Centralized here instead of duplicated per call site,
    /// so it can't drift or silently get skipped on a new endpoint.
    /// </summary>
    public static class AuditSessionExtensions
    {
        public static async Task<Guid?> ResolveSessionIdAsync(this AppDbContext context, string? jwtId)
        {
            if (string.IsNullOrEmpty(jwtId))
                return null;

            return await context.AdminSessions
                .AsNoTracking()
                .Where(x => x.JwtId == jwtId)
                .Select(x => (Guid?)x.SessionId)
                .FirstOrDefaultAsync();
        }
    }
}