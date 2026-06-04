using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Auth;

public class CandidateSendOtpRequestDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    public string? CountryCode { get; set; }
}