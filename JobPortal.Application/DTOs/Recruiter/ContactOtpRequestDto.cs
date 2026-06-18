using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Recruiter
{

    // 3A — Save contact details + send OTP
    public class ContactDetailsRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactPersonEmail { get; set; } = string.Empty;  // personal email

        [Required]
        [EmailAddress]
        public string CompanyEmail { get; set; } = string.Empty;        // corporate email

        [Required]
        [RegularExpression(@"^\+\d{1,4}$", ErrorMessage = "e.g. +91")]
        public string CountryCode { get; set; } = "+91";

        [Required]
        [RegularExpression(@"^\d{7,12}$", ErrorMessage = "Invalid mobile number.")]
        public string MobileNumber { get; set; } = string.Empty;

        public string? CompanyDescription { get; set; }
    }

    public class ContactDetailsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MaskedMobile { get; set; }   // 90*****705
        public int OtpExpiresInSeconds { get; set; } = 600;
        public StepStatusDto? StepStatus { get; set; }

    }

    // 3B — Verify OTP
    public class VerifyContactOtpRequestDto
    {
        [Required]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        public string CountryCode { get; set; } = string.Empty;

        [Required]
        public string MobileOtpCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CompanyEmail { get; set; } = string.Empty;

        [Required]
        public string EmailOtpCode { get; set; } = string.Empty;
    }

    public class VerifyContactOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? EmployerRegistrationToken { get; set; } // temp token to proceed

        public StepStatusDto? StepStatus { get; set; }
    }
}
