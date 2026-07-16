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
    public Guid? CandidateId { get; set; }
    public string? UserName { get; set; }

    public string? ProfileStatus { get; set; }

    public string? RedirectTo { get; set; }

    public DateTime? ExpiresAt { get; set; }

    // Lets the frontend hide/block restricted pages and actions without
    // waiting for a server round-trip to find out. Always true for the
    // account owner; reflects the sub-user's actual current flags otherwise.
    public bool IsSubUser { get; set; }

    public bool CanSearchCandidates { get; set; } = true;
    public bool CanUnlockProfiles { get; set; } = true;
    public bool CanPostJobs { get; set; } = true;
    public bool CanManageApplications { get; set; } = true;
}