namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    // POST /api/admin/support-tickets/{ticketId}/reply
    // This is intentionally the ONLY write action admins get on a ticket —
    // there is no accompanying status/resolve field here by design.
    public class AdminAddTicketReplyRequestDto
    {
        public string Message { get; set; } = default!;
    }
}