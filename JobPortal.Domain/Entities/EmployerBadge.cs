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
    public string BadgeType { get; set; } = default!;
    public string BadgeStatus { get; set; } = "Active";
    public bool BadgeGstVerified { get; set; } = false;
    public bool BadgePanVerified { get; set; } = false;
    public bool BadgePoeLicensed { get; set; } = false;
    public bool BadgeRpslLicensed { get; set; } = false;
    public bool BadgeBlueTick { get; set; } = false;
    public bool BlueTickEligible { get; set; } = false;
    public string? BadgeRevocationReason { get; set; }
    public Guid IssuedBy { get; set; }
    public DateTime BadgeIssuedAt { get; set; }
    public DateTime? BadgeRevokedAt { get; set; }

    // Navigation
    public EmployerProfile EmployerProfile { get; set; } = default!;
    public AdminUser IssuedByAdmin { get; set; } = default!;
}
