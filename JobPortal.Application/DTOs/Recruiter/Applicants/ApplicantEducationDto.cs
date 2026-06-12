using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class ApplicantEducationDto
    {
        public string EducationLevel { get; set; } = string.Empty;

        public string? InstituteName { get; set; }

        public short? PassoutYear { get; set; }

        public string? CertificateUrl { get; set; }
    }
}
