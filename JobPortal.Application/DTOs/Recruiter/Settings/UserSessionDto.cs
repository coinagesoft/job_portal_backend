using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class UserSessionDto
    {
        public Guid SessionId { get; set; }

        public string DeviceName { get; set; } = default!;

        public string Browser { get; set; } = default!;

        public string OperatingSystem { get; set; } = default!;

        public string Location { get; set; } = default!;

        public string IpAddress { get; set; } = default!;

        public bool IsCurrentSession { get; set; }

        public DateTime LastSeenAt { get; set; }
    }
}
