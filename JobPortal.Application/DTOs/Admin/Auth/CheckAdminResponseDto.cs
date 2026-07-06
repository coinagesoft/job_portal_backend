using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class CheckAdminResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Full E.164 number to pass into Firebase on frontend
        /// e.g. +919876543210
        /// Frontend uses this to trigger Firebase OTP
        /// </summary>
        public string? E164Number { get; set; }
    }
}
