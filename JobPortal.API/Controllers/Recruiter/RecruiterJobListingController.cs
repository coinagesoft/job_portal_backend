using JobPortal.Application.DTOs.Recruiter.JobListing;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/jobs")]
    public class RecruiterJobListingController : ControllerBase
    {
        private readonly IRecruiterJobListingService _service;

        public RecruiterJobListingController(
            IRecruiterJobListingService service)
        {
            _service = service;
        }

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
                jobId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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
                jobId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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
                jobId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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
                jobId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
    }
}