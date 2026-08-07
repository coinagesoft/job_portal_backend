using JobPortal.Domain.Constants;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobPortal.API.Middleware;

/// <summary>
/// Automatically records an AuditLog row for every successful, authenticated
/// admin-panel mutation (POST/PUT/PATCH/DELETE under /api/admin/...), so new
/// endpoints are audited without any extra code in the controller/service.
///
/// Endpoints that already write their own — richer — audit entry (e.g. Login,
/// Create Sub Admin) should be marked [SkipAuditLog] to avoid a duplicate row.
/// GET requests are never logged here — audit logs record actions, not reads.
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    // "SuspendUser" -> "Suspend User"
    private static readonly Regex PascalCaseSplitter = new("(?<!^)([A-Z])", RegexOptions.Compiled);

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        await _next(context);

        try
        {
            if (!ShouldLog(context, out var endpoint))
                return;

            var user = context.User;

            if (user?.Identity?.IsAuthenticated != true)
                return;

            var adminIdClaim = user.FindFirst("AdminId")?.Value;

            if (!Guid.TryParse(adminIdClaim, out var adminId))
                return;

            var admin = await dbContext.AdminUsers
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == adminId);

            if (admin == null)
                return;

            var controllerActionDescriptor = endpoint!
                .Metadata
                .GetMetadata<ControllerActionDescriptor>();

            var auditAttribute = endpoint.Metadata.GetMetadata<AuditLogAttribute>();

            var module = auditAttribute?.Module
                ?? controllerActionDescriptor?.ControllerName
                ?? "Admin";

            var actionName = auditAttribute?.Action
                ?? Humanize(controllerActionDescriptor?.ActionName ?? context.Request.Method);

            var success = context.Response.StatusCode is >= 200 and < 300;

            // Severity is resolved in this order, most explicit wins:
            //   1. [AuditLog(severity: ...)] on the endpoint itself
            //   2. AuditActionSeverity's explicit action -> severity map
            //      (the single source of truth for "how bad is this")
            //   3. The generic keyword/HTTP-method heuristic, only used
            //      for actions nobody has classified yet — this is what
            //      previously made every "delete"/DELETE action Critical
            //      regardless of how significant it actually was.
            var severity = auditAttribute?.Severity
                ?? (!success
                    ? AuditSeverity.Warning
                    : AuditActionSeverity.TryResolve(actionName, out var mappedSeverity)
                        ? mappedSeverity
                        : InferSeverity(context.Request.Method, actionName, success));

            var targetEntityType = controllerActionDescriptor?.ControllerName;

            Guid? targetEntityId = context.Request.RouteValues.TryGetValue("id", out var idValue)
                && Guid.TryParse(idValue?.ToString(), out var parsedId)
                    ? parsedId
                    : null;

            dbContext.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = admin.AdminId,
                PerformedByName = admin.FullName,
                PerformedByRole = admin.Role?.RoleName ?? admin.AdminType,
                Module = module,
                Action = actionName,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                Description = $"{actionName} via {context.Request.Method} {context.Request.Path}",
                IpAddress = context.GetClientIpAddress(),
                UserAgent = context.GetUserAgent(),
                Success = success,
                Severity = severity,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // Audit logging must never break or mask the actual response
            // already written to the client.
        }
    }

    private static bool ShouldLog(HttpContext context, out Microsoft.AspNetCore.Http.Endpoint? endpoint)
    {
        endpoint = context.GetEndpoint();

        if (endpoint == null)
            return false;

        if (!context.Request.Path.StartsWithSegments("/api/admin"))
            return false;

        if (!MutatingMethods.Contains(context.Request.Method))
            return false;

        if (endpoint.Metadata.GetMetadata<SkipAuditLogAttribute>() != null)
            return false;

        return true;
    }

    private static string Humanize(string pascalCase)
    {
        if (string.IsNullOrWhiteSpace(pascalCase))
            return "Action";

        return PascalCaseSplitter.Replace(pascalCase, " $1").Trim();
    }

    private static AuditSeverity InferSeverity(string httpMethod, string actionName, bool success)
    {
        if (!success)
            return AuditSeverity.Warning;

        var lowered = actionName.ToLowerInvariant();

        var criticalKeywords = new[]
        {
            "delete", "remove", "suspend", "block", "reject", "revoke",
            "deactivate", "ban"
        };

        if (criticalKeywords.Any(lowered.Contains))
            return AuditSeverity.Critical;

        if (httpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            return AuditSeverity.Critical;

        var warningKeywords = new[]
        {
            "export", "config", "setting", "update", "edit", "change",
            "reset", "override"
        };

        if (warningKeywords.Any(lowered.Contains))
            return AuditSeverity.Warning;

        return AuditSeverity.Info;
    }
}