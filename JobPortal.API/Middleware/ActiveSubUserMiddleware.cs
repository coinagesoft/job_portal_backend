using System.Security.Claims;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Middleware;

/// <summary>
/// A JWT is a stateless bearer token — once issued, there's no built-in way
/// to revoke it before it expires. That's fine for the account owner, but
/// it means a sub-user whose access the owner just deactivated or deleted
/// would otherwise keep working normally for the rest of their session,
/// only getting blocked the next time they try to log in.
///
/// This middleware closes that gap: for every authenticated request from a
/// sub-user (never the account owner), it checks the sub-user's CURRENT
/// status directly from the database. If they've been deactivated or
/// deleted, the request is rejected with 401 immediately — so their very
/// next API call (which, in a normal SPA, happens within seconds as they
/// navigate/refresh data) fails, the frontend's 401 handler logs them out,
/// and they're back at the login screen without needing to manually log
/// out first.
/// </summary>
public class ActiveSubUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveSubUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            var isSubUserClaim = user.FindFirst("IsSubUser")?.Value;

            if (roleClaim == "Recruiter" && isSubUserClaim == "true")
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var employerIdClaim = user.FindFirst("EmployerId")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId) &&
                    Guid.TryParse(employerIdClaim, out var employerId))
                {
                    var subUser = await dbContext.EmployerSubUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s =>
                            s.UserId == userId &&
                            s.EmployerId == employerId);

                    // Missing (deleted) or anything other than "Active"
                    // (Deactivated, Deleted, Pending, etc.) means this
                    // person no longer has standing access.
                    if (subUser == null || subUser.SubUserStatus != "Active")
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(
                            "{\"success\":false,\"message\":\"Your access has been revoked. Please contact your account owner.\"}");

                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}