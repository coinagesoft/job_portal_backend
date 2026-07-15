using JobPortal.Application.DTOs.Recruiter.Applicants;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/applicants")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterApplicantsController : ControllerBase
    {
        private readonly IRecruiterApplicantService _service;
        private readonly ISubUserPermissionService _permissionService;

        public RecruiterApplicantsController(
            IRecruiterApplicantService service,
            ISubUserPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        // Who's actually acting — resolved from the signed JWT, not a
        // client-supplied header, so it can't be spoofed.
        private Guid GetActionUserId() =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private bool GetIsSubUser() =>
            User.FindFirst("IsSubUser")?.Value == "true";

        // Every applicant-status-changing action below needs this. Returns
        // null when allowed; otherwise the result to return immediately.
        private async Task<IActionResult?> EnforceManageApplicationsAsync()
        {
            var check = await _permissionService.CheckAsync(
                GetActionUserId(), GetIsSubUser(), s => s.CanManageApplications);

            return check.Allowed
                ? null
                : BadRequest(new { Success = false, Message = check.Message });
        }

        // =====================================================
        // Dashboard
        // =====================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetDashboardAsync(
                    employerId);

            return Ok(result);
        }

        // =====================================================
        // Applicant List
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetApplicants(
            [FromQuery] Guid employerId,
            [FromQuery] ApplicantListRequestDto request)
        {
            var result =
                await _service.GetApplicantsAsync(
                    employerId,
                    request);

            return Ok(result);
        }

        // =====================================================
        // Applicant Details
        // =====================================================

        [HttpGet("{applicationId}")]
        public async Task<IActionResult> GetApplicantDetails(
            Guid applicationId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetApplicantDetailsAsync(
                    employerId,
                    applicationId);

            if (result == null)
            {
                return NotFound(new
                {
                    Message = "Applicant not found."
                });
            }

            return Ok(result);
        }

        // =====================================================
        // Job Wise Applicants
        // =====================================================

        [HttpGet("~/api/recruiter/jobs/{jobId}/applicants")]
        public async Task<IActionResult> GetJobApplicants(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetJobApplicantsAsync(
                    employerId,
                    jobId);

            if (result == null)
            {
                return NotFound(new
                {
                    Message = "Job not found."
                });
            }

            return Ok(result);
        }

        // =====================================================
        // Move To Review
        // =====================================================

        [HttpPatch("{applicationId}/review")]
        public async Task<IActionResult> MoveToReview(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] UpdateApplicantNoteRequestDto request)
        {
            if (await EnforceManageApplicationsAsync() is { } denied) return denied;

            var result =
                await _service.MoveToReviewAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Shortlist
        // =====================================================

        [HttpPatch("{applicationId}/shortlist")]
        public async Task<IActionResult> ShortlistApplicant(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] UpdateApplicantNoteRequestDto request)
        {
            if (await EnforceManageApplicationsAsync() is { } denied) return denied;

            var result =
                await _service.ShortlistApplicantAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Schedule Interview
        // =====================================================

        [HttpPatch("{applicationId}/interview")]
        public async Task<IActionResult> ScheduleInterview(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] ScheduleInterviewRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await EnforceManageApplicationsAsync() is { } denied) return denied;

            var result =
                await _service.ScheduleInterviewAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Reject Applicant
        // =====================================================

        [HttpPatch("{applicationId}/reject")]
        public async Task<IActionResult> RejectApplicant(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] RejectApplicantRequestDto request)
        {
            if (await EnforceManageApplicationsAsync() is { } denied) return denied;

            var result =
                await _service.RejectApplicantAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Hire Applicant
        // =====================================================

        [HttpPatch("{applicationId}/hire")]
        public async Task<IActionResult> HireApplicant(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] UpdateApplicantNoteRequestDto request)
        {
            if (await EnforceManageApplicationsAsync() is { } denied) return denied;

            var result =
                await _service.HireApplicantAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Add Recruiter Note
        // =====================================================

        [HttpPost("{applicationId}/notes")]
        public async Task<IActionResult> AddRecruiterNote(
            Guid applicationId,
            [FromQuery] Guid employerId,
            [FromBody] AddRecruiterNoteRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result =
                await _service.AddRecruiterNoteAsync(
                    employerId,
                    applicationId,
                    request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // =====================================================
        // Get Recruiter Notes
        // =====================================================

        [HttpGet("{applicationId}/notes")]
        public async Task<IActionResult> GetRecruiterNotes(
            Guid applicationId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetRecruiterNotesAsync(
                    employerId,
                    applicationId);

            return Ok(result);
        }
    }
}