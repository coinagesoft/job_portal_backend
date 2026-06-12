using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{
    public class JobListResponseDto
    {
        public int TotalRecords { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<RecruiterJobListItemDto> Jobs { get; set; }
            = new();
    }
}
