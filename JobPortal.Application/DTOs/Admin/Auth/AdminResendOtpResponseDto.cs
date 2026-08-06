using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class AdminResendOtpResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int ResendAfterSeconds { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
