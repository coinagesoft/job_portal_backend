using JobPortal.Application.DTOs.Admin.AuditLogs;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAuditLogService
    {
        // Powers the read-only GET /api/admin/audit-logs endpoint.
        Task<AuditLogListResponseDto> GetAuditLogsAsync(AuditLogRequestDto request);
    }
}