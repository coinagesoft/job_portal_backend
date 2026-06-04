using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class LinkedInLoginRequestDto
{
    /// <summary>
    /// Authorization code from LinkedIn OAuth redirect
    /// </summary>
    [Required]
    public string LinkedInCode { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI must match exactly what's registered in LinkedIn app
    /// </summary>
    [Required]
    public string RedirectUri { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserType UserType { get; set; }
}
