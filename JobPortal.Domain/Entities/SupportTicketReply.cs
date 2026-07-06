using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class SupportTicketReply
    {
        public Guid ReplyId { get; set; }

        public Guid TicketId { get; set; }

        public Guid SenderId { get; set; }

        public ReplySenderType SenderType { get; set; }

        public string Message { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public SupportTicket Ticket { get; set; } = default!;
    }
}
