using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class JobApplicationScreeningDto
    {
        public Guid ApplicationId { get; set; }

        public Guid CandidateId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public ApplicationStatus ApplicationStatus { get; set; }

        public DateTime AppliedAt { get; set; }

        public List<ScreeningQuestionAnswerDto> Screening { get; set; } = new();
    }
}
