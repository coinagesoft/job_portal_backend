using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class VerifyEmailOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string CompanyEmail { get; set; } = default!;

        [Required]
        [RegularExpression(@"^\d{6}$")]
        public string EmailOtpCode { get; set; } = default!;
    }
}
