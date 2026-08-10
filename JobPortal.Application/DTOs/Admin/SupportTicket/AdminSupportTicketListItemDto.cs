namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    public class AdminSupportTicketListItemDto
    {
        public Guid TicketId { get; set; }

        // "Candidate" | "Recruiter"
        public string RaisedByType { get; set; } = default!;

        public Guid RaisedByUserId { get; set; }

        public string RaisedByName { get; set; } = default!;

        public string? RaisedByAvatarUrl { get; set; }

        public string Category { get; set; } = default!;

        public string Subject { get; set; } = default!;

        // Short excerpt of Description for the table row — the full text
        // is only returned by the thread endpoint.
        public string DescriptionPreview { get; set; } = default!;

        public string Status { get; set; } = default!;

        public string Priority { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        // Timestamp of the last message in the thread (or CreatedAt if no
        // one has replied yet). This is what the 48h auto-resolve job
        // measures against.
        public DateTime LastActivityAt { get; set; }

        // Original ticket description counts as message #1, so a brand
        // new ticket with zero replies still shows "1 message".
        public int MessageCount { get; set; }
    }
}