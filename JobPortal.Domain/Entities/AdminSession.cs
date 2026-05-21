using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class AdminSession
{
    public Guid SessionId { get; set; }
    public Guid AdminId { get; set; }
    public string SessionToken { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public bool TrustedDevice { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public AdminUser AdminUser { get; set; } = default!;
}
