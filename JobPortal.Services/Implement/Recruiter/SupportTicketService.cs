using JobPortal.Application.DTOs.Recruiter.SupportTicket;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace JobPortal.Services.Implement.Recruiter
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly AppDbContext _context;

        public SupportTicketService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<CreateSupportTicketResponseDto> CreateTicketAsync(
            Guid employerId,
            CreateSupportTicketRequestDto request)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new CreateSupportTicketResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid(),
                RaisedBy = employer.UserId,
                TicketType = request.TicketType,
                Subject = request.Subject,
                Description = request.Description,
                Status = "Open",
                Priority = "Normal",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);

            await _context.SaveChangesAsync();

            return new CreateSupportTicketResponseDto
            {
                Success = true,
                Message = "Ticket created successfully.",
                TicketId = ticket.TicketId
            };
        }

        public async Task<SupportTicketListResponseDto> GetTicketsAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SupportTicketListResponseDto();
            }

            var tickets = await _context.SupportTickets
                .Where(x => x.RaisedBy == employer.UserId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupportTicketItemDto
                {
                    TicketId = x.TicketId,
                    TicketType = x.TicketType.ToString(),
                    Subject = x.Subject,
                    Status = x.Status,
                    Priority = x.Priority,
                    CreatedAt = x.CreatedAt,
                    ResolvedAt = x.ResolvedAt,
                    ResolutionNote = x.ResolutionNote
                })
                .ToListAsync();

            return new SupportTicketListResponseDto
            {
                TotalTickets = tickets.Count,
                Tickets = tickets
            };
        }

        public async Task<SupportTicketThreadResponseDto?> GetTicketThreadAsync(
            Guid ticketId)
        {
            var ticket = await _context.SupportTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TicketId == ticketId);

            if (ticket == null)
                return null;

            var replies = await _context.SupportTicketReplies
                .Where(x => x.TicketId == ticketId)
                .OrderBy(x => x.CreatedAt)
              .Select(x => new TicketReplyDto
              {
                  ReplyId = x.ReplyId,
                  Message = x.Message,
                  SenderType = x.SenderType,
                  CreatedAt = x.CreatedAt
              })
                .ToListAsync();

            return new SupportTicketThreadResponseDto
            {
                TicketId = ticket.TicketId,
                TicketType = ticket.TicketType.ToString(),
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                Replies = replies
            };
        }

        public async Task<AddTicketReplyResponseDto> AddReplyAsync(
            Guid ticketId,
            Guid employerId,
            AddTicketReplyRequestDto request)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new AddTicketReplyResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(x => x.TicketId == ticketId);

            if (ticket == null)
            {
                return new AddTicketReplyResponseDto
                {
                    Success = false,
                    Message = "Ticket not found."
                };
            }

            if (ticket.Status == "Resolved")
            {
                return new AddTicketReplyResponseDto
                {
                    Success = false,
                    Message = "This ticket is resolved and no longer accepts new messages. Please raise a new ticket if you need further help."
                };
            }

            var reply = new SupportTicketReply
            {
                ReplyId = Guid.NewGuid(),
                TicketId = ticketId,
                SenderId = employer.UserId,
                SenderType = ReplySenderType.Employer,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTicketReplies.Add(reply);

            // Keeps last-activity accurate for the 48h auto-resolve job
            // (SupportTicketAutoResolveService) — the candidate-side
            // AddReplyAsync already does this.
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AddTicketReplyResponseDto
            {
                Success = true,
                Message = "Reply added successfully."
            };
        }

        public async Task<bool> ResolveTicketAsync(
            Guid ticketId)
        {
            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(x => x.TicketId == ticketId);

            if (ticket == null)
                return false;

            ticket.Status = "Resolved";
            ticket.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<SupportTicketSummaryDto> GetSummaryAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SupportTicketSummaryDto();
            }

            var tickets = await _context.SupportTickets
                .Where(x => x.RaisedBy == employer.UserId)
                .ToListAsync();

            return new SupportTicketSummaryDto
            {
                TotalTickets = tickets.Count,
                Open = tickets.Count(x => x.Status == "Open"),
                InProgress = tickets.Count(x => x.Status == "InProgress"),
                Resolved = tickets.Count(x => x.Status == "Resolved")
            };
        }
    }
}