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

    public string? PaymentGateway { get; set; }

    public string? RazorpayPaymentId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string? RazorpaySignature { get; set; }

    public string? PayPalOrderId { get; set; }

    public string? PayPalCaptureId { get; set; }

    [Required]
    public bool TermsAccepted { get; set; }

    public string? CouponCode { get; set; }
}