using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class ApplicantListRequestDto
    {
        public Guid? JobId { get; set; }

        public string? Status { get; set; }

        public string? Search { get; set; }

        // Quick filters (Applicants page "Quick filters" row)
        public bool? MinExperience3Years { get; set; }
        public bool? NoticePeriodMax30Days { get; set; }
        public bool? MandatoryAnswersComplete { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}