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

        // Other candidates with the same trade — shown below the Overview
        // sidebar card, mirroring "Similar Jobs" on the candidate-facing
        // job detail page.
        public List<RelatedCandidateCardDto> RelatedCandidates { get; set; }
            = new();
    }

    public class RelatedCandidateCardDto
    {
        public Guid CandidateId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        public int TotalExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string? AvailabilityStatus { get; set; }
    }
}