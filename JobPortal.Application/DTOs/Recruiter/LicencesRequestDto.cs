using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter;

/// <summary>
/// Step 4 — Send as multipart/form-data (has file uploads)
/// </summary>
public class LicencesRequestDto
{
    [Required(ErrorMessage = "POE Licence is required.")]
    public IFormFile? PoeLicence { get; set; }

    [Required(ErrorMessage = "RPSL Licence is required.")]
    public IFormFile? RpslLicence { get; set; }
}

public class LicencesResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public string? PoeLicenceUrl { get; set; }
    public string? RpslLicenceUrl { get; set; }

    public List<string> BadgesEarned { get; set; } = new();

    public StepStatusDto? StepStatus { get; set; }
}