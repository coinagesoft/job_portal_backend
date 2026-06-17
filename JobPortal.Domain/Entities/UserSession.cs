using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{


    public class UserSession
    {
        [Key]
        public Guid SessionId { get; set; }

        public Guid UserId { get; set; }

        public string DeviceName { get; set; }
            = default!;

        public string Browser { get; set; }
            = default!;

        public string OperatingSystem { get; set; }
            = default!;

        public string Location { get; set; }
            = default!;

        public string IpAddress { get; set; }
            = default!;

        public bool IsCurrentSession { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public User User { get; set; } = default!;
    }
}
