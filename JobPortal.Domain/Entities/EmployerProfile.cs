using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class EmployerProfile
{
    public Guid EmployerId { get; set; }
    public Guid UserId { get; set; }
    public string LegalName { get; set; } = default!;
    public string? TradeName { get; set; }
    public string CompanyDisplayName { get; set; } = default!;
    public string? CompanyDescription { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public CompanySize? CompanySize { get; set; }
    public short? YearEstablished { get; set; }
    public string? WebsiteUrl { get; set; }
    public BusinessType BusinessType { get; set; } = default!;
    public IndustryType IndustryType { get; set; } = default!;
    // GST
    public bool GstRegistered { get; set; } = false;
    public string? Gstn { get; set; }
    public string? Pan { get; set; }
    public string? Cin { get; set; }
    public DateOnly? GstnRegistrationDate { get; set; }
    public string? KarzaRequestId { get; set; }
    // Address
    public string AddressLine1 { get; set; } = default!;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = default!;
    public string? State { get; set; }
    public string Pincode { get; set; } = default!;
    public string Country { get; set; } = "India";
    public string? OfficeAddress { get; set; }
    // Contact
    public string ContactPhone { get; set; } = default!;
    public string? ContactEmailPublic { get; set; }
    public string ContactPersonName { get; set; } = default!;
    public string Designation { get; set; } = default!;
    public string? OperatingHours { get; set; }
    // Status
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Pending;
    public DateTime? TrialExpiresAt { get; set; }
    public bool SecurityDepositPaid { get; set; } = false;
    public string? SecurityDepositStatus { get; set; }
    public byte ProfileCompletionScore { get; set; } = 0;
    // POE
    public string? PoeLicenceS3Url { get; set; }
    public string? PoeLicenceNumber { get; set; }
    public string? PoeCompanyName { get; set; }
    public DateOnly? PoeValidityDate { get; set; }
    public bool PoeExpiredFlag { get; set; } = false;
    // RPSL
    public string? RpslLicenceS3Url { get; set; }
    public string? RpslLicenceNumber { get; set; }
    public string? RpslCompanyName { get; set; }
    public DateOnly? RpslValidityDate { get; set; }
    public bool RpslExpiredFlag { get; set; } = false;
    // Docs
    public string? BusinessRegDocUrl { get; set; }
    public DateTime? ConsentTimestamp { get; set; }
    public string? Tags { get; set; }               // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    // Navigation
    public User User { get; set; } = default!;

    public CreditWallet? CreditWallet { get; set; }

    public ICollection<EmployerBadge> Badges { get; set; } = new List<EmployerBadge>();

    public ICollection<EmployerSubUser> SubUsers { get; set; } = new List<EmployerSubUser>();

    public EmployerNotificationSetting? NotificationSetting { get; set; }
}
