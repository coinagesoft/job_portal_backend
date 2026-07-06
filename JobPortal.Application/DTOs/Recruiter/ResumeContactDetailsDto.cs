using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ResumeContactDetailsDto
    {
        public string? ContactPersonName { get; set; }
        public string? Designation { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CountryCode { get; set; }
        public string? MobileNumber { get; set; }
        public string? CompanyDescription { get; set; }
        public bool MobileVerified { get; set; }
        public bool CompanyEmailVerified { get; set; }
    }
}
