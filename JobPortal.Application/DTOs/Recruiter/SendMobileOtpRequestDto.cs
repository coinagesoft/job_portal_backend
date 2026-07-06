using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class SendMobileOtpRequestDto
    {
        [Required]
        public string CountryCode { get; set; } = default!;

        [Required]
        public string MobileNumber { get; set; } = default!;
    }
}
