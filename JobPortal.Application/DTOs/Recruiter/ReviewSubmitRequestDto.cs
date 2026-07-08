using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter;

/// <summary>
/// Step 5 — Final submit. Send as application/json.
/// Files were already uploaded in steps 2 and 4 — pass their URLs here.
/// </summary>
public class ReviewSubmitRequestDto
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must accept terms and conditions.")]
    public bool ConsentGiven { get; set; }

    [Required]
    public string ConsentVersion { get; set; } = "v1.0";
}

/// <summary>
/// Company details for final submit — text only, no file
/// Logo URL comes from step 2 response
/// </summary>
public class CompanyDetailsSubmitDto
{
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string CompanyDisplayName { get; set; } = string.Empty;
    public string BusinessType { get; set; }      // ✅ enum
    public CompanySize? CompanySize { get; set; }       // ✅ enum
    public string? Cin { get; set; }
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// URL returned from step 2 company-details upload
    /// </summary>
    public string? CompanyLogoUrl { get; set; }         // ✅ URL not file
}

/// <summary>
/// Licences for final submit — URLs only, no files
/// URLs come from step 4 upload-licences response
/// </summary>
public class LicencesSubmitDto
{
    public bool SkipLicences { get; set; } = false;

    /// <summary>
    /// POE licence URL from step 4 response
    /// </summary>
    public string? PoeLicenceUrl { get; set; }          // ✅ URL not file

    /// <summary>
    /// RPSL licence URL from step 4 response
    /// </summary>
    public string? RpslLicenceUrl { get; set; }         // ✅ URL not file
}

public class ReviewSubmitResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? EmployerId { get; set; }
    public string? AccountStatus { get; set; }
    public bool RequiresSecurityDeposit { get; set; }
    public int? SecurityDepositAmountRs { get; set; }
    public string? NextStep { get; set; }
    public bool RegistrationCompleted { get; set; }
    public StepStatusDto? StepStatus { get; set; }
}