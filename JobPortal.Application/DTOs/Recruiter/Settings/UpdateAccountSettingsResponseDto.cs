using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class UpdateAccountSettingsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        public bool OtpRequired { get; set; }

        public string? VerificationType { get; set; }
    }
}
