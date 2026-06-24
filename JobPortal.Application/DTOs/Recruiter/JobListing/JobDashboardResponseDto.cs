using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{
    public class JobDashboardResponseDto
    {
        public int TotalJobs { get; set; }

        public int ActiveJobs { get; set; }

        public int PausedJobs { get; set; }

        public int ClosedJobs { get; set; }

        public int ArchivedJobs { get; set; }

        public int NormalJobs { get; set; }

        public int ClassifiedJobs { get; set; }

        public int HotVacancyJobs { get; set; }
        public int DraftJobs { get; set; }

        public int FeaturedJobs { get; set; }

        public int UrgentHiringJobs { get; set; }

        public int TotalApplications { get; set; }

        public int TotalViews { get; set; }
    }
}
