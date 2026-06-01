using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace JobPortal.Application.DTOs.Recruiter;

/// <summary>
/// Step 4 — Send as multipart/form-data (has file uploads)
/// </summary>
public class LicencesRequestDto
{
    /// <summary>
    /// Set true to skip licence upload — can upload later from dashboard
    /// </summary>
    public bool SkipLicences { get; set; } = false;

    /// <summary>
    /// POE / Recruitment Licence — PDF/JPG/PNG, max 5MB
    /// Awards: Recruitment Licensed badge
    /// </summary>
    public IFormFile? PoeLicence { get; set; }          // ✅ IFormFile not base64

    /// <summary>
    /// RPSL Licence — PDF/JPG/PNG, max 5MB
    /// Awards: RPSL Licensed badge
    /// </summary>
    public IFormFile? RpslLicence { get; set; }         // ✅ IFormFile not base64
}

public class LicencesResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PoeLicenceUrl { get; set; }          // S3 URL after upload
    public string? RpslLicenceUrl { get; set; }         // S3 URL after upload
    public List<string> BadgesEarned { get; set; } = new();

    public StepStatusDto? StepStatus { get; set; }
}