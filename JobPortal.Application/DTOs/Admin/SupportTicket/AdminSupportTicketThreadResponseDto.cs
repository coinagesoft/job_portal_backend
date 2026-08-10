namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    // Backs the review drawer on /admin/helpAndsupport.
    // GET /api/admin/support-tickets/{ticketId}
    public class AdminSupportTicketThreadResponseDto
    {
        public bool Success { get; set; } = true;

        public string? Message { get; set; }

        public Guid TicketId { get; set; }

        // "Candidate" | "Recruiter"
        public string RaisedByType { get; set; } = default!;

        public string RaisedByName { get; set; } = default!;

        public string? RaisedByAvatarUrl { get; set; }

        public string Category { get; set; } = default!;

        public string Subject { get; set; } = default!;

        public string Description { get; set; } = default!;

        public string Status { get; set; } = default!;

        public string Priority { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public DateTime LastActivityAt { get; set; }

        // False once the ticket is Resolved — frontend should hide/disable
        // the reply box instead of relying on a status button that doesn't
        // exist for admins.
        public bool CanReply { get; set; }

        public List<AdminTicketReplyDto> Replies { get; set; } = new();
    }
}