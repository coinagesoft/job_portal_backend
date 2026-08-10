using JobPortal.Application.DTOs.Admin.SupportTicket;
using JobPortal.Domain.Constants;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobPortal.Services.Implement.Admin
{
    public class AdminSupportTicketService : IAdminSupportTicketService
    {
        private readonly AppDbContext _context;

        public AdminSupportTicketService(AppDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════════
        // LIST
        // ════════════════════════════════════════════════════════════
        public async Task<AdminSupportTicketListResponseDto> GetTicketsAsync(
            AdminSupportTicketListRequestDto request)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var query =
                from t in _context.SupportTickets.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.RaisedBy equals u.UserId
                join cp in _context.CandidateProfiles.AsNoTracking()
                    on t.RaisedBy equals cp.UserId into cpGroup
                from cp in cpGroup.DefaultIfEmpty()
                join ep in _context.EmployerProfiles.AsNoTracking()
                    on t.RaisedBy equals ep.UserId into epGroup
                from ep in epGroup.DefaultIfEmpty()
                select new
                {
                    Ticket = t,
                    u.UserType,
                    CandidateName = cp != null ? cp.FullName : null,
                    CandidateAvatar = cp != null ? cp.ProfilePhotoUrl : null,
                    EmployerName = ep != null ? ep.CompanyDisplayName : null,
                    EmployerAvatar = ep != null ? ep.CompanyLogoUrl : null
                };

            if (!string.IsNullOrWhiteSpace(request.RaisedByType))
            {
                if (request.RaisedByType.Equals("Candidate", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.UserType == UserType.Candidate);
                }
                else if (request.RaisedByType.Equals("Recruiter", StringComparison.OrdinalIgnoreCase)
                      || request.RaisedByType.Equals("Employer", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.UserType == UserType.Recruiter);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Ticket.Status == request.Status);
            }

            if (request.Category.HasValue)
            {
                query = query.Where(x => x.Ticket.TicketType == request.Category.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.Ticket.Subject, term) ||
                    EF.Functions.ILike(x.Ticket.Description, term) ||
                    (x.CandidateName != null && EF.Functions.ILike(x.CandidateName, term)) ||
                    (x.EmployerName != null && EF.Functions.ILike(x.EmployerName, term)));
            }

            var totalCount = await query.CountAsync();

            var pageRows = await query
                .OrderByDescending(x => x.Ticket.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var ticketIds = pageRows.Select(x => x.Ticket.TicketId).ToList();

            var replyCounts = await _context.SupportTicketReplies
                .Where(r => ticketIds.Contains(r.TicketId))
                .GroupBy(r => r.TicketId)
                .Select(g => new { TicketId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TicketId, x => x.Count);

            var items = pageRows.Select(x =>
            {
                var isCandidate = x.UserType == UserType.Candidate;

                replyCounts.TryGetValue(x.Ticket.TicketId, out var replyCount);

                return new AdminSupportTicketListItemDto
                {
                    TicketId = x.Ticket.TicketId,
                    RaisedByType = isCandidate ? "Candidate" : "Recruiter",
                    RaisedByUserId = x.Ticket.RaisedBy,
                    RaisedByName = isCandidate
                        ? (x.CandidateName ?? "Candidate")
                        : (x.EmployerName ?? "Recruiter"),
                    RaisedByAvatarUrl = isCandidate ? x.CandidateAvatar : x.EmployerAvatar,
                    Category = x.Ticket.TicketType.ToString(),
                    Subject = x.Ticket.Subject,
                    DescriptionPreview = Truncate(x.Ticket.Description, 140),
                    Status = x.Ticket.Status,
                    Priority = x.Ticket.Priority,
                    CreatedAt = x.Ticket.CreatedAt,
                    ResolvedAt = x.Ticket.ResolvedAt,
                    LastActivityAt = x.Ticket.UpdatedAt ?? x.Ticket.CreatedAt,
                    // The original ticket description is message #1 in the
                    // thread view, so a fresh ticket with zero replies
                    // still reads as "1 message".
                    MessageCount = replyCount + 1
                };
            }).ToList();

            return new AdminSupportTicketListResponseDto
            {
                Success = true,
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = pageSize == 0
                    ? 0
                    : (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        // ════════════════════════════════════════════════════════════
        // SUMMARY (tab counts)
        // ════════════════════════════════════════════════════════════
        public async Task<AdminSupportTicketSummaryDto> GetSummaryAsync()
        {
            var rows = await (
                from t in _context.SupportTickets.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.RaisedBy equals u.UserId
                select new { t.Status, u.UserType }
            ).ToListAsync();

            var candidateRows = rows.Where(x => x.UserType == UserType.Candidate).ToList();
            var recruiterRows = rows.Where(x => x.UserType == UserType.Recruiter).ToList();

            return new AdminSupportTicketSummaryDto
            {
                CandidateTotal = candidateRows.Count,
                CandidateOpen = candidateRows.Count(x => x.Status == "Open"),
                CandidateInProgress = candidateRows.Count(x => x.Status == "InProgress"),
                CandidateResolved = candidateRows.Count(x => x.Status == "Resolved"),

                RecruiterTotal = recruiterRows.Count,
                RecruiterOpen = recruiterRows.Count(x => x.Status == "Open"),
                RecruiterInProgress = recruiterRows.Count(x => x.Status == "InProgress"),
                RecruiterResolved = recruiterRows.Count(x => x.Status == "Resolved")
            };
        }

        // ════════════════════════════════════════════════════════════
        // THREAD (detail + full conversation)
        // ════════════════════════════════════════════════════════════
        public async Task<AdminSupportTicketThreadResponseDto?> GetTicketThreadAsync(
            Guid ticketId)
        {
            var ticket = await _context.SupportTickets
                .AsNoTracking()
                .Include(t => t.RaisedByUser)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
                return null;

            string raisedByType;
            string raisedByName;
            string? raisedByAvatar;

            if (ticket.RaisedByUser.UserType == UserType.Candidate)
            {
                var candidate = await _context.CandidateProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == ticket.RaisedBy);

                raisedByType = "Candidate";
                raisedByName = candidate?.FullName ?? "Candidate";
                raisedByAvatar = candidate?.ProfilePhotoUrl;
            }
            else
            {
                var employer = await _context.EmployerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == ticket.RaisedBy);

                raisedByType = "Recruiter";
                raisedByName = employer?.CompanyDisplayName ?? "Recruiter";
                raisedByAvatar = employer?.CompanyLogoUrl;
            }

            var replyRows = await _context.SupportTicketReplies
                .AsNoTracking()
                .Where(r => r.TicketId == ticketId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            // Only Admin replies need a separate name lookup — every
            // candidate/recruiter reply on a ticket comes from the same
            // single raiser, so we can reuse raisedByName for those.
            var adminIds = replyRows
                .Where(r => r.SenderType == ReplySenderType.Admin)
                .Select(r => r.SenderId)
                .Distinct()
                .ToList();

            var adminNames = adminIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.AdminUsers
                    .AsNoTracking()
                    .Where(a => adminIds.Contains(a.AdminId))
                    .ToDictionaryAsync(a => a.AdminId, a => a.FullName);

            var replies = replyRows.Select(r => new AdminTicketReplyDto
            {
                ReplyId = r.ReplyId,
                Message = r.Message,
                SenderType = MapSenderType(r.SenderType),
                SenderName = r.SenderType == ReplySenderType.Admin
                    ? (adminNames.TryGetValue(r.SenderId, out var name) ? name : "Support Team")
                    : raisedByName,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new AdminSupportTicketThreadResponseDto
            {
                Success = true,
                TicketId = ticket.TicketId,
                RaisedByType = raisedByType,
                RaisedByName = raisedByName,
                RaisedByAvatarUrl = raisedByAvatar,
                Category = ticket.TicketType.ToString(),
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                LastActivityAt = ticket.UpdatedAt ?? ticket.CreatedAt,
                CanReply = ticket.Status != "Resolved",
                Replies = replies
            };
        }

        // ════════════════════════════════════════════════════════════
        // REPLY — the ONLY write action admins have on a ticket
        // ════════════════════════════════════════════════════════════
        public async Task<AdminAddTicketReplyResponseDto> AddReplyAsync(
            Guid ticketId,
            Guid adminId,
            AdminAddTicketReplyRequestDto request,
            string ipAddress,
            string? userAgent)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return ReplyFail("Message cannot be empty.");
                }

                //-------------------------------------------------------
                // 1. Actor lookup — needed for the audit log's
                //    PerformedByName / PerformedByRole, same as
                //    AdminUserService.
                //-------------------------------------------------------

                var admin = await _context.AdminUsers
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.AdminId == adminId);

                if (admin == null || !admin.IsActive)
                    return ReplyFail("Admin account not found or inactive.");

                //-------------------------------------------------------
                // 2. Load target ticket
                //-------------------------------------------------------

                var ticket = await _context.SupportTickets
                    .FirstOrDefaultAsync(t => t.TicketId == ticketId);

                if (ticket == null)
                    return ReplyFail("Ticket not found.");

                if (ticket.Status == "Resolved")
                {
                    return ReplyFail(
                        "This ticket is already resolved and no longer accepts new messages.");
                }

                //-------------------------------------------------------
                // 3. Add the reply
                //-------------------------------------------------------

                var reply = new SupportTicketReply
                {
                    ReplyId = Guid.NewGuid(),
                    TicketId = ticketId,
                    SenderId = adminId,
                    SenderType = ReplySenderType.Admin,
                    Message = request.Message.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.SupportTicketReplies.Add(reply);

                // Admin has no manual status control. The only automatic
                // transition here is Open -> InProgress the moment support
                // first responds — resolution stays entirely with the
                // ticket owner (their own Resolve button) or the 48h
                // auto-resolve job.
                var oldStatus = ticket.Status;

                if (ticket.Status == "Open")
                    ticket.Status = "InProgress";

                if (ticket.AssignedTo == null)
                    ticket.AssignedTo = adminId;

                ticket.UpdatedAt = DateTime.UtcNow;

                //-------------------------------------------------------
                // 4. Audit log — same shape as AdminUserService /
                //    AuthService: PerformedBy*, Module, Action, target
                //    identifiers, before/after snapshot, Description,
                //    IpAddress, UserAgent, Severity.
                //-------------------------------------------------------

                _context.AuditLogs.Add(new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    PerformedByAdminId = admin.AdminId,
                    PerformedByName = admin.FullName,
                    PerformedByRole = admin.Role?.RoleName ?? admin.AdminType,
                    Module = "Help & Support",
                    Action = "Reply to Ticket",
                    TargetEntityType = "SupportTicket",
                    TargetEntityId = ticket.TicketId,
                    TargetEntityName = ticket.Subject,
                    OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
                    NewValues = JsonSerializer.Serialize(new { ticket.Status, ticket.AssignedTo }),
                    Description = $"Replied to support ticket '{ticket.Subject}'.",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = true,
                    Severity = AuditActionSeverity.Resolve("Reply to Ticket"),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AdminAddTicketReplyResponseDto
                {
                    Success = true,
                    Message = "Reply sent successfully.",
                    ReplyId = reply.ReplyId,
                    CreatedAt = reply.CreatedAt
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return ReplyFail("Unable to send reply. Please try again.");
            }
        }

        private static AdminAddTicketReplyResponseDto ReplyFail(string message)
        {
            return new AdminAddTicketReplyResponseDto
            {
                Success = false,
                Message = message
            };
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════
        private static string MapSenderType(ReplySenderType type) => type switch
        {
            ReplySenderType.Candidate => "Candidate",
            ReplySenderType.Employer => "Recruiter",
            ReplySenderType.Admin => "Admin",
            _ => type.ToString()
        };

        private static string Truncate(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text.Length <= maxLength
                ? text
                : text[..maxLength].TrimEnd() + "…";
        }
    }
}