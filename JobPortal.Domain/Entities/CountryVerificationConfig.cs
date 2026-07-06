using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CountryVerificationConfig
{
    public Guid ConfigId { get; set; }
    public string CountryCode { get; set; } = default!;          // ISO e.g. IN, AE
    public string AcceptedCandidateIdTypes { get; set; } = default!;  // JSON
    public string AcceptedEmployerDocTypes { get; set; } = default!;  // JSON
    public string PrimaryBusinessVerify { get; set; } = default!;
    public bool RequireSecurityDeposit { get; set; } = false;
    public Guid ConfigUpdatedBy { get; set; }
    public DateTime ConfigUpdatedAt { get; set; }

    public AdminUser UpdatedByAdmin { get; set; } = default!;
}
