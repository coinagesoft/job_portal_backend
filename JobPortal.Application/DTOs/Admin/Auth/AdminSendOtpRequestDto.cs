using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class AdminSendOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
