using JobPortal.Application.DTOs.Admin.AuditLogs;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAuditLogService
    {
        // Powers the read-only GET /api/admin/audit-logs endpoint.
        // No filters, no pagination — returns every audit log row.
        Task<AuditLogListResponseDto> GetAuditLogsAsync();

        // Backs the "Export CSV" button on /admin/audit. Applies the same
        // filters as the on-screen filter bar (search/date/actorType/
        // severity) and returns a UTF-8 CSV file as bytes.
        Task<byte[]> ExportAuditLogsCsvAsync(AuditLogRequestDto request);
    }
}