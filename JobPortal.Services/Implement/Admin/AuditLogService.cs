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

        public async Task<AuditLogListResponseDto> GetAuditLogsAsync(AuditLogRequestDto request)
        {
            try
            {
                var page = request.Page < 1 ? 1 : request.Page;
                var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

                var query = _context.AuditLogs
                    .AsNoTracking()
                    .Include(x => x.PerformedByAdmin)
                        .ThenInclude(x => x.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.Action))
                {
                    var action = request.Action.Trim();
                    query = query.Where(x => EF.Functions.ILike(x.Action, $"%{action}%"));
                }

                if (request.Date.HasValue)
                {
                    var start = DateTime.SpecifyKind(request.Date.Value.Date, DateTimeKind.Utc);
                    var end = start.AddDays(1);
                    query = query.Where(x => x.CreatedAt >= start && x.CreatedAt < end);
                }

                if (request.ActorType.HasValue)
                {
                    query = request.ActorType.Value == AuditActorType.SubAdmin
                        ? query.Where(x => x.PerformedByAdmin.AdminType == "SubAdmin")
                        : query.Where(x => x.PerformedByAdmin.AdminType != "SubAdmin");
                }

                if (request.Severity.HasValue)
                {
                    query = query.Where(x => x.Severity == request.Severity.Value);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
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