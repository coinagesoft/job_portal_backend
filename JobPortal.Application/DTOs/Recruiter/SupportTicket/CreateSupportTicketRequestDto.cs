using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.SupportTicket
{
    public class CreateSupportTicketRequestDto
    {
        public SupportTicketType TicketType { get; set; } = default!;

        public string Subject { get; set; } = default!;

        public string Description { get; set; } = default!;
    }
}
