using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.Models;

public class RecruiterRegistrationSession
{
    public string SessionId { get; set; } = string.Empty;
    // Step 1
    public bool GstRegistered { get; set; }
    public string? Gstn { get; set; }
    public string? Pan { get; set; }
    public string? IndustryType { get; set; }       
    public bool RequiresSecurityDeposit { get; set; }
    // Step 2
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? CompanyDisplayName { get; set; }
    public string? BusinessType { get; set; }
    public string? CompanySize { get; set; }
    public string? Cin { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? CompanyLogoUrl { get; set; }
    // Step 3
    public string? ContactPersonName { get; set; }
    public string? Designation { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? CompanyEmail { get; set; }
    public string? MobileNumber { get; set; }
    public string? CountryCode { get; set; }
    public string? CompanyDescription { get; set; }
    public Guid? OtpId { get; set; }
    public bool MobileVerified { get; set; }
    public string? RegistrationToken { get; set; }
    // Step 4
    public string? PoeLicenceS3Url { get; set; }
    public string? RpslLicenceS3Url { get; set; }
    // Meta
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }         
}
