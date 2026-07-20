using System;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class RequestEmailChangeOtpRequestDto
    {
        public string NewEmail { get; set; } = default!;
    }

    public class VerifyEmailChangeOtpRequestDto
    {
        public string NewEmail { get; set; } = default!;

        public string OtpCode { get; set; } = default!;
    }
}