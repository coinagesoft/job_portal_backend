using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RecruiterJobDto
    {
        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int AppliedCount { get; set; }

        public int TotalApplications { get; set; }

        public DateTime CreatedAt { get; set; }
        public List<Guid> ApplicationIds { get; set; } = new();


        public string JobStatus { get; set; } = string.Empty;
    }
}
