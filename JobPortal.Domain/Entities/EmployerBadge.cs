using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class EmployerBadge
{
    public Guid BadgeId { get; set; }

    public Guid EmployerId { get; set; }

    public BadgeType BadgeType { get; set; }

    public BadgeStatus BadgeStatus { get; set; }

    public string? RevocationReason { get; set; }

    public Guid? IssuedBy { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    // Navigation
    public EmployerProfile EmployerProfile { get; set; } = default!;
    public AdminUser? IssuedByAdmin { get; set; }
}

