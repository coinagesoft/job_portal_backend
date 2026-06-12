using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{

    public class JobApplicantsResponseDto
    {
        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int TotalApplicants { get; set; }

        public List<ApplicantListItemDto> Applicants { get; set; }
            = new();
    }
}
