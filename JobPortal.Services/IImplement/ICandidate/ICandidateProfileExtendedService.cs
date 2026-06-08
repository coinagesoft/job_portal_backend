// ============================================================
//  JobPortal.Services/IImplement/ICandidate/
//  ICandidateProfileExtendedService.cs
//
//  Interface covering sections 3-6 of the candidate profile wizard:
//   · Work Experience
//   · Education
//   · Skills
//   · Languages
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateProfileExtendedService
{
    // ═══════════════════════════════════════════════════════
    // SECTION 3 — WORK EXPERIENCE
    // ═══════════════════════════════════════════════════════

    /// <summary>Returns all work experience entries for the candidate.</summary>
    Task<WorkExperienceListResponseDto> GetWorkExperienceAsync(Guid candidateId);

    /// <summary>Adds a new work experience entry.</summary>
    Task<WorkExperienceMutationResponseDto> AddWorkExperienceAsync(
        Guid candidateId, AddWorkExperienceRequestDto request);

    /// <summary>Updates an existing work experience entry.</summary>
    Task<WorkExperienceMutationResponseDto> UpdateWorkExperienceAsync(
        Guid candidateId, Guid workId, UpdateWorkExperienceRequestDto request);

    /// <summary>Removes a work experience entry.</summary>
    Task<WorkExperienceMutationResponseDto> DeleteWorkExperienceAsync(
        Guid candidateId, Guid workId);

    // ═══════════════════════════════════════════════════════
    // SECTION 4 — EDUCATION
    // ═══════════════════════════════════════════════════════

    /// <summary>Returns all education qualifications for the candidate.</summary>
    Task<EducationListResponseDto> GetEducationAsync(Guid candidateId);

    /// <summary>Adds a new education qualification.</summary>
    Task<EducationMutationResponseDto> AddEducationAsync(
        Guid candidateId, AddEducationRequestDto request);

    /// <summary>Updates an existing education entry.</summary>
    Task<EducationMutationResponseDto> UpdateEducationAsync(
        Guid candidateId, Guid educationId, UpdateEducationRequestDto request);

    /// <summary>Removes an education entry.</summary>
    Task<EducationMutationResponseDto> DeleteEducationAsync(
        Guid candidateId, Guid educationId);

    // ═══════════════════════════════════════════════════════
    // SECTION 5 — SKILLS
    // ═══════════════════════════════════════════════════════

    /// <summary>Returns all skills (SkillType == "Skill") for the candidate.</summary>
    Task<SkillsListResponseDto> GetSkillsAsync(Guid candidateId);

    /// <summary>Adds a single skill.</summary>
    Task<SkillMutationResponseDto> AddSkillAsync(
        Guid candidateId, AddSkillRequestDto request);

    /// <summary>Updates an existing skill's name, proficiency, and years.</summary>
    Task<SkillMutationResponseDto> UpdateSkillAsync(
        Guid candidateId, Guid skillId, UpdateSkillRequestDto request);

    /// <summary>Removes a skill.</summary>
    Task<SkillMutationResponseDto> DeleteSkillAsync(
        Guid candidateId, Guid skillId);

    /// <summary>
    /// Replaces the entire skill list in one operation.
    /// Used by the wizard's "Tap to select skills + set proficiency" screen.
    /// </summary>
    Task<BulkSaveSkillsResponseDto> BulkSaveSkillsAsync(
        Guid candidateId, BulkSaveSkillsRequestDto request);

    // ═══════════════════════════════════════════════════════
    // SECTION 6 — LANGUAGES
    // ═══════════════════════════════════════════════════════

    /// <summary>Returns all language preferences (SkillType == "Language") for the candidate.</summary>
    Task<LanguagesListResponseDto> GetLanguagesAsync(Guid candidateId);

    /// <summary>Adds a language preference.</summary>
    Task<LanguageMutationResponseDto> AddLanguageAsync(
        Guid candidateId, AddLanguageRequestDto request);

    /// <summary>Updates an existing language preference.</summary>
    Task<LanguageMutationResponseDto> UpdateLanguageAsync(
        Guid candidateId, Guid skillId, UpdateLanguageRequestDto request);

    /// <summary>Removes a language preference.</summary>
    Task<LanguageMutationResponseDto> DeleteLanguageAsync(
        Guid candidateId, Guid skillId);
}