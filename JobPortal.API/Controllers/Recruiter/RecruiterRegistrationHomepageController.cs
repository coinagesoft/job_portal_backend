// ============================================================
//  JobPortal.API/Controllers/Recruiter/RecruiterRegistrationHomepageController.cs
//
//  Adds the Industry Type dropdown + "Other" suggestion endpoints for
//  Employer Registration Step 1 (the GST check step). Kept as its own
//  controller class so it can be dropped in without touching your
//  existing RecruiterRegistrationController.cs — both share the same
//  "api/recruiter/registration" route prefix and none of the paths
//  collide with your existing gst-check / company-details / etc actions.
//
//  If you'd rather have these two actions live directly inside
//  RecruiterRegistrationController, just copy the two [HttpGet]/[HttpPost]
//  methods below into that class instead and delete this file.
//
//  Anonymous — registration Step 1 happens before any recruiter account
//  or session contact info exists.
// ============================================================

using JobPortal.Application.DTOs.Recruiter.Homepage;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/registration")]
    [Produces("application/json")]
    public class RecruiterRegistrationHomepageController : ControllerBase
    {
        private static readonly string[] AllowedFields = { "Industry" };

        private readonly IRecruiterHomepageService _service;

        public RecruiterRegistrationHomepageController(IRecruiterHomepageService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/recruiter/registration/industries
        /// Step 1 "Industry Type" dropdown — active-only, admin display
        /// order. Append your own "Other" entry on the frontend; picking
        /// it should reveal the free-text field that posts below.
        /// </summary>
        [HttpGet("industries")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RecruiterIndustriesResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIndustries()
        {
            var result = await _service.GetRegistrationIndustriesAsync();
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        /// <summary>
        /// POST api/recruiter/registration/industry-suggestions
        /// Submits a value picked via "Other" on the Step 1 Industry Type
        /// field. Body: { suggestedName, note?, submittedByName?, submittedByEmail? }
        /// (Field is fixed to "Industry" server-side — no need to send it.)
        /// Shows up in the admin Suggestions inbox; approving it there adds
        /// the value straight into the Industry Type dropdown above.
        /// </summary>
        [HttpPost("industry-suggestions")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RecruiterSuggestionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitIndustrySuggestion([FromBody] RecruiterSuggestionRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SuggestedName))
                return BadRequest(new RecruiterSuggestionResponseDto { Success = false, Message = "SuggestedName is required." });

            request.Field = "Industry";

            var result = await _service.SubmitSuggestionAsync(request, submittedByUserId: null, AllowedFields);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}