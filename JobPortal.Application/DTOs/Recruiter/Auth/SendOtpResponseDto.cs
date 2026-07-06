using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class SendOtpResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MaskedIdentifier { get; set; }  // ad***@gmail.com or ****3210
    public string IdentifierType { get; set; } = string.Empty; // "email" or "mobile"
    public int ExpiresInSeconds { get; set; } = 600;
    public int ResendCooldownSeconds { get; set; } = 60;
}
