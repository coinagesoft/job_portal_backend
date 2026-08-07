using JobPortal.Application.DTOs.Admin.SupportTicket;

namespace JobPortal.Services.IImplement.IAdmin
{
    // Admin side of Help & Support. By design this interface has NO
    // resolve/status endpoint — admins can only read tickets and reply
    // in the chat. Tickets close either because the ticket owner
    // (candidate/recruiter) marks them resolved from their own panel,
    // or automatically after 48 hours of inactivity
    // (see SupportTicketAutoResolveService).
    public interface IAdminSupportTicketService
    {
        Task<AdminSupportTicketListResponseDto> GetTicketsAsync(
            AdminSupportTicketListRequestDto request);

        Task<AdminSupportTicketSummaryDto> GetSummaryAsync();

        Task<AdminSupportTicketThreadResponseDto?> GetTicketThreadAsync(
            Guid ticketId);

        Task<AdminAddTicketReplyResponseDto> AddReplyAsync(
            Guid ticketId,
            Guid adminId,
            AdminAddTicketReplyRequestDto request);
    }
}