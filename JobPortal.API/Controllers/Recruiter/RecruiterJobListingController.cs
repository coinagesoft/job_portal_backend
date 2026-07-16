using JobPortal.Application.DTOs.Recruiter.JobListing;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/jobs")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterJobListingController : ControllerBase
    {
        private readonly IRecruiterJobListingService _service;

        public RecruiterJobListingController(
            IRecruiterJobListingService service)
        {
            _service = service;
        }

        // Who's actually acting — resolved from the signed JWT, not a
        // client-supplied header, so it can't be spoofed.
        private Guid GetActionUserId() =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private bool GetIsSubUser() =>
            User.FindFirst("IsSubUser")?.Value == "true";

        // =====================================================
        // Dashboard
        // =====================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] Guid employerId)
        {
            var result = await _service.GetDashboardAsync(
                employerId);

            return Ok(result);
        }

        // =====================================================
        // Job List
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetJobs(
            [FromQuery] Guid employerId,
            [FromQuery] JobListRequestDto request)
        {
            var result = await _service.GetJobsAsync(
                employerId,
                request);

            return Ok(result);
        }

        // =====================================================
        // Job Details
        // =====================================================

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobById(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.GetJobByIdAsync(
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
        // Job Stats
        // =====================================================

        [HttpGet("{jobId}/stats")]
        public async Task<IActionResult> GetJobStats(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.GetJobStatsAsync(
                employerId,
                jobId);

            return Ok(result);
        }

        // =====================================================
        // Pause Job
        // =====================================================

        [HttpPatch("{jobId}/pause")]
        public async Task<IActionResult> PauseJob(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.PauseJobAsync(
                employerId,
                jobId,
                GetActionUserId(),
                GetIsSubUser());

            return result.Success
                ? Ok(result)
                : (result.Message.Contains("permission") || result.Message.Contains("deactivated") || result.Message.Contains("not accepted")
                    ? StatusCode(403, result)
                    : BadRequest(result));
        }

        // =====================================================
        // Resume Job
        // =====================================================

        [HttpPatch("{jobId}/resume")]
        public async Task<IActionResult> ResumeJob(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.ResumeJobAsync(
                employerId,
                jobId,
                GetActionUserId(),
                GetIsSubUser());

            return result.Success
                ? Ok(result)
                : (result.Message.Contains("permission") || result.Message.Contains("deactivated") || result.Message.Contains("not accepted")
                    ? StatusCode(403, result)
                    : BadRequest(result));
        }

        // =====================================================
        // Close Job
        // =====================================================

        [HttpPatch("{jobId}/close")]
        public async Task<IActionResult> CloseJob(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.CloseJobAsync(
                employerId,
                jobId,
                GetActionUserId(),
                GetIsSubUser());

            return result.Success
                ? Ok(result)
                : (result.Message.Contains("permission") || result.Message.Contains("deactivated") || result.Message.Contains("not accepted")
                    ? StatusCode(403, result)
                    : BadRequest(result));
        }

        // =====================================================
        // Archive Job
        // =====================================================

        [HttpPatch("{jobId}/archive")]
        public async Task<IActionResult> ArchiveJob(
            Guid jobId,
            [FromQuery] Guid employerId)
        {
            var result = await _service.ArchiveJobAsync(
                employerId,
                jobId,
                GetActionUserId(),
                GetIsSubUser());

            return result.Success
                ? Ok(result)
                : (result.Message.Contains("permission") || result.Message.Contains("deactivated") || result.Message.Contains("not accepted")
                    ? StatusCode(403, result)
                    : BadRequest(result));
        }
    }
}