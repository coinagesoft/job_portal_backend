using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class LogoutResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
