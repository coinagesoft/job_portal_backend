using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Auth;

public class CandidateVerifyOtpRequestDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    [Required]
    public string OtpCode { get; set; } = string.Empty;
}