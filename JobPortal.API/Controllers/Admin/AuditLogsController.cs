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

        // GET /api/admin/audit-logs
        // Backs the table on /admin/audit. No filters or pagination —
        // returns every audit log row in one call. Read-only — this is
        // the only endpoint exposed for audit logs.
        [HttpGet]
        [SkipAuditLog]
        public async Task<IActionResult> GetAuditLogs()
        {
            var result = await _auditLogService.GetAuditLogsAsync();

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Export CSV

        // GET /api/admin/audit-logs/export/csv?action=&date=&actorType=&severity=
        // Backs the "Export CSV" button on /admin/audit. Same filters as
        // the on-screen filter bar; returns every matching row (no
        // pagination) as a downloadable CSV file.
        [HttpGet("export/csv")]
        [SkipAuditLog]
        public async Task<IActionResult> ExportCsv([FromQuery] AuditLogRequestDto request)
        {
            var bytes = await _auditLogService.ExportAuditLogsCsvAsync(request);
            var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        #endregion
    }
}