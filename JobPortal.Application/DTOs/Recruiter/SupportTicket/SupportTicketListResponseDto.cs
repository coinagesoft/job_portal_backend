using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class SupportTicketListResponseDto
    {
        public int TotalTickets { get; set; }

        public List<SupportTicketItemDto> Tickets { get; set; } = new();
    }
}
