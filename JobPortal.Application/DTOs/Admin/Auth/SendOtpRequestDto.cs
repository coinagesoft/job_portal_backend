namespace JobPortal.Application.DTOs.Auth;

public class SendOtpRequestDto
{
    public string MobileNumber { get; set; } = default!;

    public string CountryCode { get; set; } = default!;

    public string UserType { get; set; } = default!;
}