using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CvSearchDashboardDto
    {
        public int TotalCandidates { get; set; }

        public int BandA { get; set; }

        public int BandB { get; set; }

        public int BandC { get; set; }
    }
}
