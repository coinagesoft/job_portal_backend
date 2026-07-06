using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{
    public class JobStatsResponseDto
    {
        public int Applied { get; set; }

        public int InReview { get; set; }

        public int Shortlisted { get; set; }

        public int Interview { get; set; }

        public int Rejected { get; set; }

        public int Hired { get; set; }

        public int Withdrawn { get; set; }

        public int TotalApplications { get; set; }
    }
}
