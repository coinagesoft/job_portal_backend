using JobPortal.Application.DTOs.Admin.AuditLogs;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;

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
                        NewValues = x.NewValues
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
    }
}