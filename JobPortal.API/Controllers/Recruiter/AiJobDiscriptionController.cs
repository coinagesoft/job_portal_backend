using JobPortal.Application.DTOs.Recruiter.AIJobDescription;
using JobPortal.Services.IImplement.AI;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

/// <summary>
/// AI writing assistant for the job-posting form.
///
/// TWO FLOWS:
///
/// Flow A — Auto-generate (called once, all meta-fields filled)
///   POST /api/recruiter/ai/job-description/auto-generate
///   Frontend calls this the moment the employer fills:
///     JobTitle, Role, TradeCategory, ExperienceYears, JobType, EmploymentType
///   Returns a complete, structured JD + skill suggestions.
///   Drop the description straight into the textarea.
///
/// Flow B — Inline suggestions (called on debounced keyup ~600 ms)
///   POST /api/recruiter/ai/job-description/inline-suggest
///   Frontend sends what the employer has typed so far.
///   Returns 3 short continuation sentences shown as ghost-text / dropdown.
///
/// Flow C — Skill chips (Step 3, unchanged)
///   POST /api/recruiter/ai/job-description/suggest-skills
/// </summary>
[ApiController]
[Route("api/recruiter/ai/job-description")]
public class AiJobDescriptionController : ControllerBase
{
    private readonly IAiJobDescriptionService _service;

    public AiJobDescriptionController(IAiJobDescriptionService service)
        => _service = service;

    // =====================================================
    // FLOW A — Auto-generate full JD
    // =====================================================

    /// <summary>
    /// Call this once all form meta-fields are filled.
    /// No "Generate" button needed — trigger from frontend when
    /// the last required field (e.g. EmploymentType) loses focus.
    ///
    /// Returns:
    ///   - generatedDescription : drop straight into the JD textarea
    ///   - suggestedSkills      : pre-fill Step 3 skill chips
    /// </summary>
    [HttpPost("auto-generate")]
    public async Task<IActionResult> AutoGenerate(
        [FromBody] AutoGenerateJdRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle) ||
            string.IsNullOrWhiteSpace(request.TradeCategory))
        {
            return BadRequest(new
            {
                Success = false,
                Message = "JobTitle and TradeCategory are required."
            });
        }

        var result = await _service.AutoGenerateAsync(request);

        return result.Success
            ? Ok(result)
            : StatusCode(500, result);
    }

    // =====================================================
    // FLOW B — Inline suggestions while typing
    // =====================================================

    /// <summary>
    /// Call on debounced keyup (recommended: 600 ms) while the employer
    /// types in the Job Description textarea.
    ///
    /// Returns 3 short next-sentence suggestions.
    /// Show them as:
    ///   - ghost/overlay text (press Tab to accept)
    ///   - a small dropdown beneath the cursor
    ///   - clickable suggestion chips below the textarea
    ///
    /// The endpoint returns an empty list if the current text is
    /// too short (&lt;20 chars) — safe to call without extra guards.
    /// </summary>
    [HttpPost("inline-suggest")]
    public async Task<IActionResult> InlineSuggest(
        [FromBody] JdInlineSuggestionRequestDto request)
    {
        var result = await _service.GetInlineSuggestionsAsync(request);
        // Always 200 — frontend just shows nothing if list is empty
        return Ok(result);
    }

    // =====================================================
    // FLOW C — Skill chip suggestions  (Step 3)
    // =====================================================

    /// <summary>
    /// Returns 10–15 skill suggestions for the given job title + trade.
    /// Optionally pass the written description for higher accuracy.
    /// </summary>
    [HttpPost("suggest-skills")]
    public async Task<IActionResult> SuggestSkills(
        [FromBody] AiSkillSuggestionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle))
            return BadRequest(new { Success = false, Message = "JobTitle is required." });

        var result = await _service.SuggestSkillsAsync(request);
        return result.Success ? Ok(result) : StatusCode(500, result);
    }
}
