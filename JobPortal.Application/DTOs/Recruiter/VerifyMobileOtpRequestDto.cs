using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class VerifyMobileOtpRequestDto
    {
        [Required]
        public string CountryCode { get; set; } = default!;

        [Required]
        public string MobileNumber { get; set; } = default!;

        [Required]
        [RegularExpression(@"^\d{6}$")]
        public string MobileOtpCode { get; set; } = default!;
    }
}
