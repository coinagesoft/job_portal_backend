using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/verification")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterVerificationController : ControllerBase
    {
        private readonly IVerificationService _verificationService;

        public RecruiterVerificationController(IVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        /// <summary>
        /// Get verification dashboard
        /// </summary>
        [HttpGet("{employerId:guid}")]
        public async Task<IActionResult> GetVerificationDashboard(Guid employerId)
        {
            // This is re-fetched immediately after every document upload —
            // if any intermediate cache (browser, CDN/edge) were to serve a
            // stale response for this URL, it would look exactly like "the
            // upload succeeded but the document isn't in the list."
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            var result = await _verificationService
                .GetVerificationDashboardAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Employer profile not found."
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Upload verification document
        /// </summary>
        [HttpPost("{employerId:guid}/document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(
            Guid employerId,
            [FromForm] UploadVerificationDocumentRequestDto request)
        {
            // Only the account owner may upload/replace verification
            // documents — sub-users can still view the dashboard and any
            // already-uploaded documents via the GET endpoints above.
            if (User.FindFirst("IsSubUser")?.Value == "true")
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = "You don't have permission to upload verification documents. Please contact your account owner."
                });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Please select a file."
                });
            }

            var result = await _verificationService
                .UploadDocumentAsync(
                    employerId,
                    request);

            if (!result)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Employer profile not found."
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Document uploaded successfully."
            });
        }

        /// <summary>
        /// View uploaded document
        /// </summary>
        [HttpGet("document-types")]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var result = await _verificationService.GetDocumentTypesAsync();

            return Ok(new
            {
                Success = true,
                Data = result
            });
        }
    }
}