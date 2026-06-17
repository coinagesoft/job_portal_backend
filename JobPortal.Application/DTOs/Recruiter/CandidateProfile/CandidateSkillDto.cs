using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateSkillDto
    {
        public string SkillName { get; set; } = string.Empty;

        public byte? YearsOfExperience { get; set; }

        public string? SkillRole { get; set; }
    }
}
