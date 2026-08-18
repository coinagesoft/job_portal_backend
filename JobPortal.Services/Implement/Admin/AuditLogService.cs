using JobPortal.Application.DTOs.Admin.AuditLogs;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using JobPortal.Services.IImplement.IAdmin;

namespace JobPortal.Services.Implement.Admin
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogListResponseDto> GetAuditLogsAsync()
        {
            try
            {
                var items = await _context.AuditLogs
                    .AsNoTracking()
                    .Include(x => x.PerformedByAdmin)
                        .ThenInclude(x => x.User)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new AuditLogItemDto
                    {
                        LogId = x.LogId,
                        Timestamp = x.CreatedAt,
                        Admin = x.PerformedByAdmin.User.Email ?? x.PerformedByName,
                        ActorType = x.PerformedByAdmin.AdminType == "SubAdmin" ? "Sub-Admin" : "Admin",
                        Action = x.Action,
                        Module = x.Module,
                        TargetEntity = x.TargetEntityName ?? x.TargetEntityType,
                        IpAddress = x.IpAddress,
                        Severity = x.Severity.ToString(),
                        Success = x.Success,
                        Description = x.Description,
                        UserAgent = x.UserAgent,
                        OldValues = x.OldValues,
                        NewValues = x.NewValues,
                        SessionId = x.SessionId
                    })
                    .ToListAsync();

                return new AuditLogListResponseDto
                {
                    Success = true,
                    Items = items,
                    TotalCount = items.Count
                };
            }
            catch (Exception)
            {
                return new AuditLogListResponseDto
                {
                    Success = false,
                    Message = "Failed to load audit logs."
                };
            }
        }

        // ------------------------------------------------------------
        // EXPORT CSV
        // ------------------------------------------------------------
        // Applies the same filters as the /admin/audit filter bar
        // (search over Action, calendar-day Date, ActorType, Severity)
        // and streams every matching row back as a CSV file. No
        // pagination here on purpose — an export is expected to contain
        // everything that matches the filters, not just the current page.
        public async Task<byte[]> ExportAuditLogsCsvAsync(AuditLogRequestDto request)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .Include(x => x.PerformedByAdmin)
                    .ThenInclude(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                query = query.Where(x =>
                    EF.Functions.Like(x.Action, $"%{request.Action}%") ||
                    EF.Functions.Like(x.PerformedByName, $"%{request.Action}%") ||
                    (x.SessionId != null && EF.Functions.Like(x.SessionId.ToString()!, $"%{request.Action}%")));
            }

            if (request.Date.HasValue)
            {
                var dayStart = request.Date.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                query = query.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
            }

            if (request.ActorType.HasValue)
            {
                var adminType = request.ActorType.Value == AuditActorType.SubAdmin ? "SubAdmin" : "Admin";
                query = query.Where(x => x.PerformedByAdmin.AdminType == adminType);
            }

            if (request.Severity.HasValue)
            {
                query = query.Where(x => x.Severity == request.Severity.Value);
            }

            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.CreatedAt,
                    Admin = x.PerformedByAdmin.User.Email ?? x.PerformedByName,
                    ActorType = x.PerformedByAdmin.AdminType == "SubAdmin" ? "Sub-Admin" : "Admin",
                    x.Action,
                    x.Module,
                    TargetEntity = x.TargetEntityName ?? x.TargetEntityType,
                    x.IpAddress,
                    Severity = x.Severity.ToString(),
                    x.Success,
                    x.SessionId,
                    x.Description
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",",
                "Timestamp", "Admin", "Actor Type", "Action", "Module",
                "Target Entity", "IP Address", "Severity", "Success",
                "Session", "Description"));

            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",",
                    CsvField(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvField(row.Admin),
                    CsvField(row.ActorType),
                    CsvField(row.Action),
                    CsvField(row.Module),
                    CsvField(row.TargetEntity),
                    CsvField(row.IpAddress),
                    CsvField(row.Severity),
                    CsvField(row.Success ? "Success" : "Failed"),
                    CsvField(row.SessionId?.ToString()),
                    CsvField(row.Description)));
            }

            // UTF-8 BOM so Excel opens the file with correct encoding
            // instead of mangling non-ASCII characters.
            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(csv.ToString());
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

            return bytes;
        }

        // Wraps a field in quotes and escapes embedded quotes whenever the
        // value could otherwise break the CSV structure (comma, quote, or
        // newline) — keeps plain values unquoted for a smaller/cleaner file.
        private static string CsvField(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}