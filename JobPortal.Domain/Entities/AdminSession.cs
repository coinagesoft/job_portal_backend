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

    // JWT Id (jti claim)
    public string JwtId { get; set; } = default!;

    // Refresh Token
    public string RefreshToken { get; set; } = default!;

    public DateTime RefreshTokenExpiresAt { get; set; }

    public string IpAddress { get; set; } = default!;

    public string? UserAgent { get; set; }

    public bool TrustedDevice { get; set; } = false;

    public DateTime LoginAt { get; set; }

    public DateTime? LogoutAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    // Navigation
    public AdminUser AdminUser { get; set; } = default!;
}
