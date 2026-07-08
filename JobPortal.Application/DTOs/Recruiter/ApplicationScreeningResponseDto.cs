using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ApplicationScreeningResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid ApplicationId { get; set; }

        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public Guid CandidateId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public List<ScreeningQuestionAnswerDto> Screening { get; set; } = new();
    }
}
