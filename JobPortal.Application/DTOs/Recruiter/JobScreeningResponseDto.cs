using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class JobScreeningResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int TotalApplications { get; set; }

        public List<JobApplicationScreeningDto> Applications { get; set; } = new();
    }
}
