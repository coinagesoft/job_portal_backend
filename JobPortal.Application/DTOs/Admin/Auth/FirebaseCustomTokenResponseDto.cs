using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class FirebaseCustomTokenResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        public string? FirebaseToken { get; set; }
        public string? PhoneUsed { get; set; }
    }
}
