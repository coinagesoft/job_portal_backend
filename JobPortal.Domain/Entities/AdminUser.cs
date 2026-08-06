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

    // Example: ADM-000001
    public string AdminIdentifier { get; set; } = default!;

    // SuperAdmin | SubAdmin
    public string AdminType { get; set; } = default!;

    // FK -> AdminRole
    public Guid RoleId { get; set; }

    // Optional JSON to override role permissions
    public string? PermissionOverrides { get; set; }

    public short FailedAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = default!;

    public AdminRole Role { get; set; } = default!;

    public AdminUser? CreatedByAdmin { get; set; }

    public ICollection<AdminUser> CreatedAdmins { get; set; } = new List<AdminUser>();

    public ICollection<AdminSession> Sessions { get; set; } = new List<AdminSession>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
