using JobPortal.Domain.Enums.common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using JobPortal.Domain.Enums.Common;

namespace JobPortal.Application.DTOs.Recruiter;

/// <summary>
/// Step 2 — Send as multipart/form-data (has file upload)
/// </summary>
public class CompanyDetailsRequestDto
{
    [Required(ErrorMessage = "Legal company name is required.")]
    public string LegalName { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    [Required(ErrorMessage = "Display name is required.")]
    public string CompanyDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Legal business structure — dropdown in Swagger
    /// </summary>
    [Required(ErrorMessage = "Business type is required.")]
    public string BusinessType { get; set; }

    /// <summary>
    /// Company size range — dropdown in Swagger
    /// </summary>
    public CompanySize? CompanySize { get; set; }       // ✅ enum nullable

    /// <summary>
    /// Company Identification Number (optional — only for Pvt Ltd / LLP)
    /// </summary>
    public string? Cin { get; set; }

    // ── NEW fields from UI ─────────────────────────
    public string? Gstn { get; set; }
    public string? Pan { get; set; }
    public DateOnly? GstnRegistrationDate { get; set; }

    [Required(ErrorMessage = "State is required.")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Defaults to India since the registration form pre-fills/locks this
    /// field, but is still accepted from the client rather than hardcoded
    /// here, so a future non-India employer isn't silently overwritten.
    /// </summary>
    public string? Country { get; set; } = "India";

    [Required(ErrorMessage = "PIN code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN must be 6 digits.")]
    public string Pincode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full registered address is required.")]
    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Company logo file — PNG/JPG only, max 2MB
    /// </summary>
    public IFormFile? CompanyLogo { get; set; }         // ✅ IFormFile for upload

    /// <summary>
    /// "RecruitmentAgency" or "Employer" — drives which licence uploads
    /// are shown/required on the Step 4 Licences screen (Recruitment
    /// License only applies to agencies; Certificate of Incorporation
    /// applies to everyone).
    /// </summary>
    [Required(ErrorMessage = "Nature of company is required.")]
    public string NatureOfCompany { get; set; } = string.Empty;

    /// <summary>
    /// Whether this recruiter places candidates internationally — asked
    /// regardless of NatureOfCompany, since both a Recruitment Agency and
    /// a direct Employer can place candidates abroad. When true, POE
    /// License and RPSL License are also shown/required on Step 4.
    /// Nullable so the model binder can distinguish "not answered yet"
    /// (null — should never reach here since the frontend blocks
    /// Continue until answered, but caught defensively in the controller)
    /// from an explicit "No" (false).
    /// </summary>
    public bool? PlacesCandidatesInternationally { get; set; }
}

public class CompanyDetailsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }         // S3 URL after upload

    public StepStatusDto? StepStatus { get; set; }
}