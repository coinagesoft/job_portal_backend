using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class EmployerSubUser
{
    public Guid SubUserId { get; set; }
    public Guid EmployerId { get; set; }
    public Guid UserId { get; set; }
    public string SubUserName { get; set; } = default!;
    public string SubUserEmail { get; set; } = default!;

    public string? SubUserMobile { get; set; }        
    public string? SubUserCountryCode { get; set; }
    public string SubUserRole { get; set; } = default!;  
    public Guid? InviteToken { get; set; }
    public DateTime? InviteExpiresAt { get; set; }
    public bool InviteAccepted { get; set; } = false;
    public bool CanSearchCandidates { get; set; } = true;
    public bool CanUnlockProfiles { get; set; } = false;
    public bool CanPostJobs { get; set; } = false;
    public bool CanManageApplications { get; set; } = false;
    public string SubUserStatus { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    // Navigation
    public EmployerProfile EmployerProfile { get; set; } = default!;
    public User User { get; set; } = default!;
}
