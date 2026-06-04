using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.Recruiter;

public class GstCheckRequestDto
{
    /// <summary>
    /// Is your company GST registered? true = Yes, false = No
    /// </summary>
    [Required(ErrorMessage = "Please select GST registration status.")]
    public bool GstRegistered { get; set; }

    /// <summary>
    /// Select your industry type — dropdown in Swagger
    /// </summary>
    [Required(ErrorMessage = "Industry type is required.")]
    public IndustryType IndustryType { get; set; }   // ✅ from RecruiterEnums.cs
}

public class GstCheckResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool GstRegistered { get; set; }
    public string IndustryType { get; set; } = string.Empty;
    public string? RegistrationSessionId { get; set; }

    public StepStatusDto? StepStatus { get; set; }
}
