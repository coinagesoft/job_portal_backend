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

public class GoogleLoginRequestDto
{
    /// <summary>
    /// ID token from Google Sign-In on frontend
    /// </summary>
    [Required]
    public string GoogleIdToken { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserType UserType { get; set; }
}
