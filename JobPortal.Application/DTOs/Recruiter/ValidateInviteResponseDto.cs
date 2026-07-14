using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ValidateInviteResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string SubUserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
    }
}
