using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class JobVerificationDto
    {
        public bool IsOffshore { get; set; }

        public bool RpslRequired { get; set; }

        public bool RpslVerified { get; set; }

        public string? RpslStatus { get; set; }
    }
}
