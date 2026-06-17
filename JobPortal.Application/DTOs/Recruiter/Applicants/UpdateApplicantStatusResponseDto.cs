using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{

    public class UpdateApplicantStatusResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid ApplicationId { get; set; }

        public string ApplicationStatus { get; set; } = string.Empty;
    }
}
