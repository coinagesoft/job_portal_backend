using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{
    public class AdminAuditContext
    {
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = default!;
        public string AdminRole { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public string? UserAgent { get; set; }
    }
}
