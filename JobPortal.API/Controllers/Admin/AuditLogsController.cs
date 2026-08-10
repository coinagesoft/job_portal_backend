using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.AuditLogs;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        #region Get Audit Logs

        // GET /api/admin/audit-logs?action=&date=&actorType=&severity=&page=&pageSize=
        // Backs the filter bar + table on /admin/audit. Read-only —
        // this is the only endpoint exposed for audit logs.
        [HttpGet]
        [SkipAuditLog]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] AuditLogRequestDto request)
        {
            var result = await _auditLogService.GetAuditLogsAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}