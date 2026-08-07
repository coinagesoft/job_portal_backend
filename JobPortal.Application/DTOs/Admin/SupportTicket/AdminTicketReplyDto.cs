namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    public class AdminTicketReplyDto
    {
        public Guid ReplyId { get; set; }

        public string Message { get; set; } = default!;

        // "Candidate" | "Recruiter" | "Admin"
        public string SenderType { get; set; } = default!;

        public string SenderName { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}