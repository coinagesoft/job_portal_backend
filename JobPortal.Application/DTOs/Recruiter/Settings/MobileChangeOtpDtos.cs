using System;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class RequestMobileChangeOtpRequestDto
    {
        public string NewMobileNumber { get; set; } = default!;

        public string NewCountryCode { get; set; } = default!;
    }

    public class VerifyMobileChangeOtpRequestDto
    {
        public string NewMobileNumber { get; set; } = default!;

        public string NewCountryCode { get; set; } = default!;

        public string OtpCode { get; set; } = default!;
    }
}