using System.Security.Claims;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Middleware;

/// <summary>
/// A JWT is a stateless bearer token — once issued, there's no built-in way
/// to revoke it before it expires. This middleware closes that gap for
/// every authenticated recruiter request by checking the CURRENT status
/// directly from the database, for two cases:
///
///  1. Sub-users — blocked the instant the account owner deactivates or
///     removes them (SubUserStatus != "Active").
///  2. The account owner themselves — blocked the instant they use
///     Settings ▸ Deactivate Account or Delete Account (User/Employer
///     AccountStatus becomes Suspended or Deleted), instead of staying
///     logged in until their token naturally expires.
///
/// Either way the request is rejected with 401 immediately — so the very
/// next API call (which, in a normal SPA, happens within seconds as the
/// page navigates/refreshes data) fails, the frontend's 401 handler logs
/// the user out, and they land back on the login screen.
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
                        await RejectAsync(
                            context,
                            "Your access has been revoked. Please contact your account owner.");

                        return;
                    }
                }
            }
            else if (roleClaim == "Recruiter")
            {
                // Account owner (not a sub-user) — check their own status.
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var ownerUser = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (ownerUser == null ||
                        ownerUser.AccountStatus == AccountStatus.Suspended ||
                        ownerUser.AccountStatus == AccountStatus.Deleted)
                    {
                        var message = ownerUser?.AccountStatus == AccountStatus.Deleted
                            ? "This account has been deleted."
                            : "This account has been deactivated. Contact support to reactivate it.";

                        await RejectAsync(context, message);

                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    private static async Task RejectAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            $"{{\"success\":false,\"message\":\"{message}\"}}");
    }
}