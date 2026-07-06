using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class AdminUser
{
    public Guid AdminId { get; set; }
    public Guid UserId { get; set; }
    public string AdminIdentifier { get; set; } = default!;  // SB-ADMIN-01
    public string AdminRole { get; set; } = default!;        // Super_Admin | Moderator | Finance_Admin | Support_Agent
    public string? Permissions { get; set; }                 // JSON string
    public short FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }                     // FK → admin_users.admin_id
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = default!;
}
