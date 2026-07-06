using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class SendOtpRequestDto
{
    /// <summary>
    /// Email address OR mobile number (digits only, no country code)
    /// </summary>
    [Required(ErrorMessage = "Email or mobile number is required.")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Required only when identifier is a mobile number.
    /// e.g. +91, +971, +1
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Candidate or Employer
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserType UserType { get; set; }
}


