using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{
    public class AdminAuditContext
    {
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = default!;
        public string AdminRole { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public string? UserAgent { get; set; }

        // The access token's "jti" claim for the request that triggered
        // this action. Used to resolve the AdminSession (see
        // AppDbContext.ResolveSessionIdAsync) so the resulting AuditLog
        // row can be attributed to a session, same as the generic
        // AuditLogMiddleware does for auto-logged actions.
        public string? JwtId { get; set; }
    }
}