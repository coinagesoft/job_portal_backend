namespace JobPortal.Application.DTOs.Auth;

public class VerifyOtpRequestDto
{
    public string MobileNumber { get; set; } = default!;

    public string CountryCode { get; set; } = default!;

    public string OtpCode { get; set; } = default!;
}