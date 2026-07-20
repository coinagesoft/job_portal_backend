using System;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class SettingsOtpResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        public int? OtpExpiresInSeconds { get; set; }
    }
}