using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class SupportTicketItemDto
    {
        public Guid TicketId { get; set; }

        public string TicketType { get; set; } = default!;

        public string Subject { get; set; } = default!;

        public string Status { get; set; } = default!;

        public string Priority { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public string? ResolutionNote { get; set; }
    }
}
