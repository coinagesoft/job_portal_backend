using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class SupportTicketSummaryDto
    {
        public int TotalTickets { get; set; }

        public int Open { get; set; }

        public int InProgress { get; set; }

        public int Resolved { get; set; }
    }
}
