using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateLanguagesResponseDto
    {
        public List<CandidateLanguageDto> Languages { get; set; }
            = new();
    }
}
