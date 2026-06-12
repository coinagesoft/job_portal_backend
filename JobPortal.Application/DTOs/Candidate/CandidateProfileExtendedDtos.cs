// ============================================================
//  JobPortal.Application/DTOs/Candidate/Profile/
//  CandidateProfileExtendedDtos.cs
//
//  Covers four profile wizard sections visible in the UI:
//   · Section 3 — Work Experience
//   · Section 4 — Education
//   · Section 5 — Skills
//   · Section 6 — Languages
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Profile;

// ─────────────────────────────────────────────────────────────
// SECTION 3 — WORK EXPERIENCE
// GET  /api/candidate/profile/work-experience
// POST /api/candidate/profile/work-experience
// PUT  /api/candidate/profile/work-experience/{workId}
// DELETE /api/candidate/profile/work-experience/{workId}
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Full list of work history entries for a candidate.
/// GET /api/candidate/profile/work-experience
/// </summary>
public class WorkExperienceListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<WorkExperienceItemDto> Data { get; set; } = new();
}

public class WorkExperienceItemDto
{
    public Guid WorkId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? WorkLocation { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    /// <summary>true = "Currently working here" checkbox is checked.</summary>
    public bool IsCurrent { get; set; }
    public string? JobDescription { get; set; }
    public bool IsOffshore { get; set; }
}

/// <summary>
/// Request body for adding a new work-experience entry.
/// POST /api/candidate/profile/work-experience
/// </summary>
public class AddWorkExperienceRequestDto
{
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? WorkLocation { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    /// <summary>Required when IsCurrent = false.</summary>
    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; } = false;

    [MaxLength(2000)]
    public string? JobDescription { get; set; }

    public bool IsOffshore { get; set; } = false;
}

/// <summary>
/// Request body for updating an existing work-experience entry.
/// PUT /api/candidate/profile/work-experience/{workId}
/// </summary>
public class UpdateWorkExperienceRequestDto
{
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? WorkLocation { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; } = false;

    [MaxLength(2000)]
    public string? JobDescription { get; set; }

    public bool IsOffshore { get; set; } = false;
}

/// <summary>
/// Shared response for add / update / delete operations on work experience.
/// </summary>
public class WorkExperienceMutationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? WorkId { get; set; }   // null on delete
    public byte ProfileCompletionPct { get; set; }
}

// ─────────────────────────────────────────────────────────────
// SECTION 4 — EDUCATION
// GET    /api/candidate/profile/education
// POST   /api/candidate/profile/education
// PUT    /api/candidate/profile/education/{educationId}
// DELETE /api/candidate/profile/education/{educationId}
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Full list of education qualifications for a candidate.
/// GET /api/candidate/profile/education
/// </summary>
public class EducationListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<EducationItemDto> Data { get; set; } = new();
}

public class EducationItemDto
{
    public Guid EducationId { get; set; }
    /// <summary>e.g. "ITI – Electrician Trade", "SSC – 10th Standard"</summary>
    public string QualificationDegree { get; set; } = string.Empty;  // maps to EducationLevel
    public string? InstituteName { get; set; }                      // maps to InstituteName (Board)
    /// <summary>Free-text year/details, e.g. "Passed: 2014 | Cert No: ITI/2014/PUN/7823"</summary>
    public string? YearDetails { get; set; }                      // rendered from PassoutYear + MarksPercentage
    public string? CertificateUrl { get; set; }
    public string? CertificateNumber { get; set; }
    /// <summary>True when the certificate has been AI-verified (AI Verified badge).</summary>
    public bool IsAiVerified { get; set; }
}

/// <summary>
/// Request body for adding an education qualification.
/// POST /api/candidate/profile/education
/// </summary>
public class AddEducationRequestDto
{
    /// <summary>
    /// Qualification / degree label shown in UI —
    /// e.g. "ITI – Electrician Trade", "SSC – 10th Standard", "Diploma in Electrical Engg."
    /// Maps to CandidateEducation.EducationLevel
    /// </summary>
    [Required, MaxLength(300)]
    public string QualificationDegree { get; set; } = string.Empty;

    /// <summary>Institute name / board, e.g. "Govt. Industrial Training Institute, Pune"</summary>
    [MaxLength(300)]
    public string? InstituteName { get; set; }

    /// <summary>
    /// Free-text year/details field shown in UI,
    /// e.g. "Passed: 2014 | Cert No: ITI/2014/PUN/7823" or "Passed: 2012"
    /// Stored in MarksPercentage column; could also carry a passout year.
    /// </summary>
    [MaxLength(500)]
    public string? YearDetails { get; set; }
    public bool IsAiVerified { get; set; }
    public short? PassoutYear { get; set; }

    /// <summary>Certificate / Roll number printed on the certificate, e.g. "ITI/2014/PUN/7823"</summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }
}

/// <summary>
/// Request body for updating an education entry.
/// PUT /api/candidate/profile/education/{educationId}
/// </summary>
public class UpdateEducationRequestDto
{
    [Required, MaxLength(300)]
    public string QualificationDegree { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? InstituteName { get; set; }

    [MaxLength(500)]
    public string? YearDetails { get; set; }
    public bool IsAiVerified { get; set; }
    public short? PassoutYear { get; set; }

    /// <summary>Certificate / Roll number printed on the certificate, e.g. "ITI/2014/PUN/7823"</summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }
}

/// <summary>
/// Shared response for add / update / delete operations on education.
/// </summary>
public class EducationMutationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? EducationId { get; set; }
    public byte ProfileCompletionPct { get; set; }
}

// ─────────────────────────────────────────────────────────────
// SECTION 5 — SKILLS
// GET    /api/candidate/profile/skills
// POST   /api/candidate/profile/skills        (add one skill)
// PUT    /api/candidate/profile/skills/{skillId}
// DELETE /api/candidate/profile/skills/{skillId}
// POST   /api/candidate/profile/skills/bulk   (replace entire set)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Full list of skills for a candidate with proficiency data.
/// GET /api/candidate/profile/skills
/// </summary>
public class SkillsListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SkillsListData? Data { get; set; }
}

public class SkillsListData
{
    public List<SkillItemDto> Skills { get; set; } = new();
    public int TotalCount { get; set; }
}

public class SkillItemDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    /// <summary>"Skill" or "Language" — this endpoint only returns SkillType == "Skill".</summary>
    public string SkillType { get; set; } = "Skill";
    /// <summary>Proficiency level: "Beginner" | "Intermediate" | "Expert"</summary>
    public string? ProficiencyLevel { get; set; }   // maps to SkillRole
    public byte? YearsOfExperience { get; set; }
}

/// <summary>
/// Add a single skill.
/// POST /api/candidate/profile/skills
/// </summary>
public class AddSkillRequestDto
{
    [Required, MaxLength(100)]
    public string SkillName { get; set; } = string.Empty;

    /// <summary>"Beginner" | "Intermediate" | "Expert"</summary>
    [MaxLength(50)]
    public string? ProficiencyLevel { get; set; }

    [Range(0, 50)]
    public byte? YearsOfExperience { get; set; }
}

/// <summary>
/// Update an existing skill's proficiency / years.
/// PUT /api/candidate/profile/skills/{skillId}
/// </summary>
public class UpdateSkillRequestDto
{
    [Required, MaxLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ProficiencyLevel { get; set; }

    [Range(0, 50)]
    public byte? YearsOfExperience { get; set; }
}

/// <summary>
/// Replace the entire skill set in one round-trip (used by the
/// "Tap to select skills + set proficiency" screen in the wizard).
/// POST /api/candidate/profile/skills/bulk
/// </summary>
public class BulkSaveSkillsRequestDto
{
    [Required]
    public List<AddSkillRequestDto> Skills { get; set; } = new();
}

/// <summary>Shared response for skill mutations.</summary>
public class SkillMutationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public byte ProfileCompletionPct { get; set; }
}

/// <summary>Response for bulk skill save.</summary>
public class BulkSaveSkillsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SavedCount { get; set; }
    public byte ProfileCompletionPct { get; set; }
}

// ─────────────────────────────────────────────────────────────
// SECTION 6 — LANGUAGES
// GET    /api/candidate/profile/languages
// POST   /api/candidate/profile/languages
// PUT    /api/candidate/profile/languages/{skillId}
// DELETE /api/candidate/profile/languages/{skillId}
// ─────────────────────────────────────────────────────────────

/// <summary>
/// All language preferences for a candidate.
/// GET /api/candidate/profile/languages
/// </summary>
public class LanguagesListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<LanguageItemDto> Data { get; set; } = new();
}

public class LanguageItemDto
{
    public Guid SkillId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    /// <summary>Proficiency level: "Native" | "Professional" | "Conversational" | "Basic"</summary>
    public string ProficiencyLevel { get; set; } = string.Empty;  // stored in SkillRole
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanSpeak { get; set; }
}

/// <summary>
/// Add a language.
/// POST /api/candidate/profile/languages
/// </summary>
public class AddLanguageRequestDto
{
    [Required, MaxLength(100)]
    public string LanguageName { get; set; } = string.Empty;

    /// <summary>"Native" | "Professional" | "Conversational" | "Basic"</summary>
    [Required, MaxLength(50)]
    public string ProficiencyLevel { get; set; } = "Conversational";

    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanSpeak { get; set; } = true;
}

/// <summary>
/// Update an existing language entry.
/// PUT /api/candidate/profile/languages/{skillId}
/// </summary>
public class UpdateLanguageRequestDto
{
    [Required, MaxLength(100)]
    public string LanguageName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ProficiencyLevel { get; set; } = "Conversational";

    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanSpeak { get; set; } = true;
}

/// <summary>Shared response for language mutations.</summary>
public class LanguageMutationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public byte ProfileCompletionPct { get; set; }
}