using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateSkill
{
    public Guid SkillId { get; set; }
    public Guid CandidateId { get; set; }
    public string SkillName { get; set; } = default!;
    public string SkillType { get; set; } = default!;  // Skill | Language
    public byte? YearsOfExperience { get; set; }
    public string? SkillRole { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
