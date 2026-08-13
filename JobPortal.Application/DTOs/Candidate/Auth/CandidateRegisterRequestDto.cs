using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Auth;

public class CandidateRegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string OtpToken { get; set; } = string.Empty;

    [Required]
    public string CountryCode { get; set; } = string.Empty;

    [Required]
    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public string RazorpayPaymentId { get; set; } = string.Empty;

    [Required]
    public string RazorpayOrderId { get; set; } = string.Empty;

    [Required]
    public string RazorpaySignature { get; set; } = string.Empty;

    [Required]
    public bool TermsAccepted { get; set; }
}