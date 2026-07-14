using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class SocialVerifyResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? AccessToken { get; set; } // only populated for LinkedIn (see below)
    }
}
