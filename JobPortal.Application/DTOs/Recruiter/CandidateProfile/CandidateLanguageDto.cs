using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateLanguageDto
    {
        public string Language { get; set; } = string.Empty;

        public bool? CanRead { get; set; }

        public bool? CanWrite { get; set; }

        public bool? CanSpeak { get; set; }
    }
}
