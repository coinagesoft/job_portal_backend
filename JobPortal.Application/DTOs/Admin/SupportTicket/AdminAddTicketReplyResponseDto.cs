namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    public class AdminAddTicketReplyResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        public Guid ReplyId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}