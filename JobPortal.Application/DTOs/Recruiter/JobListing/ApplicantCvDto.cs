using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{

    public class ApplicantCvDto
    {
        public Guid CvId { get; set; }

        public string? CvFileUrl { get; set; }

        public string? CvPdfUrl { get; set; }

        public DateTime? GeneratedAt { get; set; }
    }
}
