using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Token { get; set; }
    public string? OtpToken { get; set; }

    public Guid? UserId { get; set; }

    public Guid? EmployerId { get; set; }

    public string? UserType { get; set; }

    public string? UserName { get; set; }

    public string? ProfileStatus { get; set; }

    public string? RedirectTo { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
