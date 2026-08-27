using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class RegistrationSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();

    // Which portal: Recruiter | Candidate
    public string SessionType { get; set; } = "Recruiter";

    // Tracks furthest completed step: 0,1,2,3,4,5
    public int CurrentStep { get; set; } = 0;
    public int LastCompletedStep { get; set; } = 0;

    // Step 1 data
    public bool? GstRegistered { get; set; }
    public string? IndustryType { get; set; }
    public bool RequiresSecurityDeposit { get; set; }

    // Step 2 data
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? CompanyDisplayName { get; set; }
    public string? BusinessType { get; set; }
    public string? CompanySize { get; set; }
    public string? Cin { get; set; }
    // Add these to RegistrationSession.cs
    public string? Gstn { get; set; }
    public string? Pan { get; set; }
    public DateOnly? GstnRegistrationDate { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Pincode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? CompanyLogoUrl { get; set; }

    // "RecruitmentAgency" | "Employer" — decides which licence uploads
    // (Recruitment License) are required on Step 4, on top of Certificate
    // of Incorporation which every registrant needs.
    public string? NatureOfCompany { get; set; }

    // Asked regardless of NatureOfCompany — both a Recruitment Agency and
    // a direct Employer can place candidates abroad. When true, POE
    // License + RPSL License are also required on Step 4.
    public bool? PlacesCandidatesInternationally { get; set; }

    // Step 3 data
    public string? ContactPersonName { get; set; }
    public string? Designation { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? CompanyEmail { get; set; }
    public string? MobileNumber { get; set; }
    public string? CountryCode { get; set; }
    public string? CompanyDescription { get; set; }
    public bool MobileVerified { get; set; } = false;

    public bool CompanyEmailVerified { get; set; }
    // Step 4 data
    public string? PoeLicenceUrl { get; set; }
    public string? RpslLicenceUrl { get; set; }
    public bool LicencesSkipped { get; set; } = false;
    public string? CompanyLogoPublicId { get; set; }

    public string? PoeLicencePublicId { get; set; }

    public string? RpslLicencePublicId { get; set; }
    // Meta
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public ICollection<RegistrationSessionDocument> Documents { get; set; }
    = new List<RegistrationSessionDocument>();
}