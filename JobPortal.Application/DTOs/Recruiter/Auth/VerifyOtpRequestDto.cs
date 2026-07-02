using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class VerifyOtpRequestDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
    public string OtpCode { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserType UserType { get; set; }
}
