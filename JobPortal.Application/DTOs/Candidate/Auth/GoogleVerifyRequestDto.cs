using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class GoogleVerifyRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = default!;
    }
}
