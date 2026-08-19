// ============================================================
//  JobPortal.API/Controllers/Recruiter/RecruiterJobPostingHomepageController.cs
//
//  Adds the Trade/Role + Department dropdown + "Other" suggestion
//  endpoints for the Job Posting form. Kept as its own controller class
//  so it can be dropped in without touching your existing
//  RecruiterJobPostingController.cs — both share the same
//  "api/recruiter/jobs" route prefix and neither path collides with your
//  existing job-posting actions (e.g. search-roles, step1-job-details).
//
//  If you'd rather have these two actions live directly inside
//  RecruiterJobPostingController, just copy the two [HttpGet]/[HttpPost]
//  methods below into that class instead and delete this file.
//
//  GetDropdowns and SubmitSuggestion are [AllowAnonymous]: both are used
//  on the recruiter *registration* form too, before any JWT exists, not
//  just on the post-registration Job Posting form. See the per-action
//  comments below for details.
// ============================================================

using JobPortal.Application.DTOs.Recruiter.Homepage;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/jobs")]
    [Authorize(Roles = "Recruiter")]
    [Produces("application/json")]
    public class RecruiterJobPostingHomepageController : ControllerBase
    {
        private static readonly string[] AllowedFields = { "TradeRole", "Department" };

        private readonly IRecruiterHomepageService _service;

        public RecruiterJobPostingHomepageController(IRecruiterHomepageService service)
        {
            _service = service;
        }

        private Guid? GetActionUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
        }

        /// <summary>
        /// GET api/recruiter/jobs/dropdowns
        /// Job posting "Trade/Role" and "Department" dropdowns —
        /// active-only, admin display order. Append your own "Other" entry
        /// to each list on the frontend; picking it should reveal the
        /// free-text field that posts below.
        ///
        /// [AllowAnonymous]: this same dropdown is also used on the
        /// recruiter *registration* form (job-role/department fields
        /// collected before the account — and therefore any JWT — exists).
        /// The controller-level [Authorize(Roles = "Recruiter")] would
        /// otherwise 401/403 that call. The data returned is public,
        /// read-only reference data (no recruiter-specific info), so
        /// opening just this action is safe.
        /// </summary>
        [HttpGet("dropdowns")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RecruiterJobPostingDropdownsResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDropdowns()
        {
            var result = await _service.GetJobPostingDropdownsAsync();
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        /// <summary>
        /// POST api/recruiter/jobs/suggestions
        /// Submits a value picked via "Other" on the job posting form.
        /// Body: { field: "TradeRole" | "Department", suggestedName, note?, submittedByName?, submittedByEmail? }
        /// Shows up in the admin Suggestions inbox; approving it there adds
        /// the value straight into the matching dropdown above.
        ///
        /// [AllowAnonymous]: same reasoning as GetDropdowns above — a
        /// recruiter filling out job-role/department fields during
        /// registration may not have a token yet. GetActionUserId()
        /// already returns Guid? and SubmitSuggestionAsync accepts a null
        /// submitter, so an anonymous submission degrades gracefully
        /// (just recorded without an attributed recruiter) instead of
        /// failing outright.
        /// </summary>
        [HttpPost("suggestions")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RecruiterSuggestionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitSuggestion([FromBody] RecruiterSuggestionRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SuggestedName))
                return BadRequest(new RecruiterSuggestionResponseDto { Success = false, Message = "SuggestedName is required." });

            var result = await _service.SubmitSuggestionAsync(request, GetActionUserId(), AllowedFields);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}