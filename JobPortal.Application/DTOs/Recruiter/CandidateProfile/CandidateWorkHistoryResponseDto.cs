using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateWorkHistoryResponseDto
    {
        public List<CandidateWorkHistoryDto> WorkHistories { get; set; }
            = new();
    }
}
