using JobPortal.Application.DTOs.Recruiter.SupportTicket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ISupportTicketService
    {
        Task<CreateSupportTicketResponseDto> CreateTicketAsync(
           Guid employerId,
           CreateSupportTicketRequestDto request);

        Task<SupportTicketListResponseDto> GetTicketsAsync(
            Guid employerId);

        Task<SupportTicketThreadResponseDto?> GetTicketThreadAsync(
            Guid ticketId);

        Task<AddTicketReplyResponseDto> AddReplyAsync(
            Guid ticketId,
             Guid employerId,
            AddTicketReplyRequestDto request);

        Task<bool> ResolveTicketAsync(
            Guid ticketId);

        Task<SupportTicketSummaryDto> GetSummaryAsync(
            Guid employerId);
    }
}
