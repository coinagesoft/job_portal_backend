using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class AdminVerifyOtpResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }

        public AdminProfileDto? Admin { get; set; }

        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
