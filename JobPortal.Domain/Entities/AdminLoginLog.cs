using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{

    public class AdminLoginLog
    {
        public Guid LoginLogId { get; set; }

        public Guid AdminId { get; set; }

        public string Email { get; set; } = default!;

        public string IpAddress { get; set; } = default!;

        public string? UserAgent { get; set; }

        public DateTime LoginAt { get; set; }

        public DateTime? LogoutAt { get; set; }

        public bool IsSuccess { get; set; }

        public string? FailureReason { get; set; }

        public AdminUser AdminUser { get; set; } = default!;
    }
}
