using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter;

public class InviteSubUserResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SubUserId { get; set; }
    public string? SubUserName { get; set; }
    public string? Role { get; set; }
    public PermissionsDto? Permissions { get; set; }
    public DateTime? InviteExpiresAt { get; set; }
}

public class PermissionsDto
{
    public bool CanSearchCandidates { get; set; }
    public bool CanUnlockProfiles { get; set; }
    public bool CanPostJobs { get; set; }
    public bool CanManageApplications { get; set; }
}
