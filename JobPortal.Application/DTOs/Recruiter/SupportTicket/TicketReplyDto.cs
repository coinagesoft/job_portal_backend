using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class TicketReplyDto
    {
        public Guid ReplyId { get; set; }

        public string Message { get; set; } = default!;

        public ReplySenderType SenderType { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
