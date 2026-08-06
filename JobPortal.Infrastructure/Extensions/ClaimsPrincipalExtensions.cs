using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Infrastructure.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetAdminId(this ClaimsPrincipal user)
        {
            var adminId = user.FindFirst("AdminId")?.Value;

            if (string.IsNullOrWhiteSpace(adminId))
                throw new UnauthorizedAccessException("AdminId claim not found.");

            if (!Guid.TryParse(adminId, out var parsedAdminId))
                throw new UnauthorizedAccessException("Invalid AdminId claim.");

            return parsedAdminId;
        }
    }
}
