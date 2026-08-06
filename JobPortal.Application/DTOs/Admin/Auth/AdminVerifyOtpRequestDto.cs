using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class AdminVerifyOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
