using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class VerificationBadgeDto
    {
        public string BadgeName { get; set; } = default!;

        public string Status { get; set; } = default!;

        public string Description { get; set; } = default!;
    }
}
