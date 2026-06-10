using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class AddTicketReplyResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;
    }
}
