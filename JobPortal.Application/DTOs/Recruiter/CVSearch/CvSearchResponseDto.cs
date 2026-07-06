using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CvSearchResponseDto
    {
        public int TotalCandidates { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<CvSearchCandidateCardDto> Candidates { get; set; }
            = new();
    }
}
