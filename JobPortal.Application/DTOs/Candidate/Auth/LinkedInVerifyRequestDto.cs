using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class LinkedInVerifyRequestDto
    {
        [Required]
        public string LinkedInCode { get; set; } = default!;
        [Required]
        public string RedirectUri { get; set; } = default!;
    }
}
