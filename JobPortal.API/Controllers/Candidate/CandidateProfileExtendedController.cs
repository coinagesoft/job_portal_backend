// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateProfileExtendedController.cs
//
//  REST endpoints for the four remaining profile-wizard sections:
//   · Section 3 — Work Experience    /api/candidate/profile/work-experience
//   · Section 4 — Education          /api/candidate/profile/education
//   · Section 5 — Skills             /api/candidate/profile/skills
//   · Section 6 — Languages          /api/candidate/profile/languages
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/profile")]
[Produces("application/json")]
// [Authorize(Roles = "Candidate")]   // Uncomment once JWT auth middleware is wired up
public class CandidateProfileExtendedController : ControllerBase
{
    private readonly ICandidateProfileExtendedService _service;
    private readonly ILogger<CandidateProfileExtendedController> _logger;

    public CandidateProfileExtendedController(
        ICandidateProfileExtendedService service,
        ILogger<CandidateProfileExtendedController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Resolves CandidateId from JWT claim; falls back to query param for dev.</summary>
    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════
    // SECTION 3 — WORK EXPERIENCE
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all work experience entries for the candidate, most recent first.
    /// </summary>
    [HttpGet("work-experience")]
    [ProducesResponseType(typeof(WorkExperienceListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkExperience([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetWorkExperienceAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Adds a new work experience entry.
    /// To flag "Currently working here", set IsCurrent = true and omit EndDate.
    /// </summary>
    [HttpPost("work-experience")]
    [ProducesResponseType(typeof(WorkExperienceMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddWorkExperience(
        [FromBody] AddWorkExperienceRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.AddWorkExperienceAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing work experience entry.
    /// </summary>
    [HttpPut("work-experience/{workId:guid}")]
    [ProducesResponseType(typeof(WorkExperienceMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkExperience(
        Guid workId,
        [FromBody] UpdateWorkExperienceRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdateWorkExperienceAsync(id, workId, request);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Removes a work experience entry.
    /// </summary>
    [HttpDelete("work-experience/{workId:guid}")]
    [ProducesResponseType(typeof(WorkExperienceMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkExperience(
        Guid workId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.DeleteWorkExperienceAsync(id, workId);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // SECTION 4 — EDUCATION
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all education qualifications for the candidate, most recent first.
    /// QualificationDegree maps to EducationLevel; YearDetails carries the free-text
    /// year / certificate-number string shown in the UI.
    /// </summary>
    [HttpGet("education")]
    [ProducesResponseType(typeof(EducationListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEducation([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetEducationAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Adds a new education qualification.
    /// </summary>
    [HttpPost("education")]
    [ProducesResponseType(typeof(EducationMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddEducation(
        [FromBody] AddEducationRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.AddEducationAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing education entry.
    /// </summary>
    [HttpPut("education/{educationId:guid}")]
    [ProducesResponseType(typeof(EducationMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEducation(
        Guid educationId,
        [FromBody] UpdateEducationRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdateEducationAsync(id, educationId, request);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Removes an education entry.
    /// </summary>
    [HttpDelete("education/{educationId:guid}")]
    [ProducesResponseType(typeof(EducationMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEducation(
        Guid educationId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.DeleteEducationAsync(id, educationId);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // SECTION 5 — SKILLS
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all skills (SkillType == "Skill") for the candidate.
    /// ProficiencyLevel is one of: "Beginner", "Intermediate", "Expert".
    /// </summary>
    [HttpGet("skills")]
    [ProducesResponseType(typeof(SkillsListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkills([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetSkillsAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Adds a single skill.  Use /skills/bulk to replace the entire skill set at once.
    /// </summary>
    [HttpPost("skills")]
    [ProducesResponseType(typeof(SkillMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSkill(
        [FromBody] AddSkillRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.AddSkillAsync(id, request);

        if (!result.Success && result.Message.Contains("already been added"))
            return Conflict(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing skill's name, proficiency, and years of experience.
    /// </summary>
    [HttpPut("skills/{skillId:guid}")]
    [ProducesResponseType(typeof(SkillMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSkill(
        Guid skillId,
        [FromBody] UpdateSkillRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdateSkillAsync(id, skillId, request);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Removes a skill.
    /// </summary>
    [HttpDelete("skills/{skillId:guid}")]
    [ProducesResponseType(typeof(SkillMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSkill(
        Guid skillId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.DeleteSkillAsync(id, skillId);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Replaces the candidate's entire skill list in a single request.
    /// Ideal for the wizard's "Tap to select + set proficiency" screen where
    /// the frontend sends the complete final set after the user is done editing.
    /// </summary>
    [HttpPost("skills/bulk")]
    [ProducesResponseType(typeof(BulkSaveSkillsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSaveSkills(
        [FromBody] BulkSaveSkillsRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.BulkSaveSkillsAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // SECTION 6 — LANGUAGES
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all language preferences for the candidate.
    /// ProficiencyLevel is one of: "Native", "Professional", "Conversational", "Basic".
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(LanguagesListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLanguages([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetLanguagesAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Adds a language preference.
    /// Set CanRead / CanWrite / CanSpeak to match the checkbox state in the UI.
    /// </summary>
    [HttpPost("languages")]
    [ProducesResponseType(typeof(LanguageMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLanguage(
        [FromBody] AddLanguageRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.AddLanguageAsync(id, request);

        if (!result.Success && result.Message.Contains("already been added"))
            return Conflict(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing language preference.
    /// </summary>
    [HttpPut("languages/{skillId:guid}")]
    [ProducesResponseType(typeof(LanguageMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLanguage(
        Guid skillId,
        [FromBody] UpdateLanguageRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdateLanguageAsync(id, skillId, request);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Removes a language preference.
    /// </summary>
    [HttpDelete("languages/{skillId:guid}")]
    [ProducesResponseType(typeof(LanguageMutationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLanguage(
        Guid skillId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.DeleteLanguageAsync(id, skillId);

        if (!result.Success && result.Message.Contains("not found"))
            return NotFound(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}