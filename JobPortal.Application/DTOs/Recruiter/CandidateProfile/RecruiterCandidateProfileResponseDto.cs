using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class RecruiterCandidateProfileResponseDto
    {
        public CandidateOverviewResponseDto Overview { get; set; }
            = new();

        public CandidateSummaryResponseDto Summary { get; set; }
            = new();

        public List<CandidateSkillDto> Skills { get; set; }
            = new();

        public List<CandidateLanguageDto> Languages { get; set; }
            = new();

        public List<CandidateEducationDto> Educations { get; set; }
            = new();

        public List<CandidateWorkHistoryDto> WorkHistories { get; set; }
            = new();

        public CandidateCvResponseDto? Cv { get; set; }

        public CandidateUnlockStatusResponseDto UnlockStatus { get; set; }
            = new();
    }
}
