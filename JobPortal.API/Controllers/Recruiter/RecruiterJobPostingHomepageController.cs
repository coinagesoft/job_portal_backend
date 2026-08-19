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
//  Requires a logged-in recruiter — matches the [Authorize(Roles =
//  "Recruiter")] already on RecruiterJobPostingController, since job
//  posting only happens after registration is complete.
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
        /// </summary>
        [HttpGet("dropdowns")]
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
        /// </summary>
        [HttpPost("suggestions")]
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