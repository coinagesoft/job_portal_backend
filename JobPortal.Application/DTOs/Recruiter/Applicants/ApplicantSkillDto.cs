using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class ApplicantSkillDto
    {
        public string SkillName { get; set; } = string.Empty;

        public string SkillType { get; set; } = string.Empty;

        public byte? YearsOfExperience { get; set; }
    }
}
