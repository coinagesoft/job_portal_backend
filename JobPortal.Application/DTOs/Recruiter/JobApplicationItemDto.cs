using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class JobApplicationItemDto
    {
        public Guid ApplicationId { get; set; }

        public Guid CandidateId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string ApplicationStatus { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; }
    }
}
