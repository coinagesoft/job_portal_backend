using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/cv-search")]
    public class RecruiterCvSearchController : ControllerBase
    {
        private readonly IRecruiterCvSearchService _service;

        public RecruiterCvSearchController(
            IRecruiterCvSearchService service)
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
            var result =
                await _service.GetDashboardAsync(
                    employerId);

            return Ok(result);
        }

        // =====================================================
        // Search Candidates
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SearchCandidates(
            [FromQuery] Guid employerId,
            [FromQuery] CvSearchRequestDto request)
        {
            var result =
                await _service.SearchCandidatesAsync(
                    employerId,
                    request);

            return Ok(result);
        }

        // =====================================================
        // Candidate Preview
        // =====================================================

        [HttpGet("{candidateId}/preview")]
        public async Task<IActionResult> GetCandidatePreview(
            Guid candidateId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetCandidatePreviewAsync(
                    employerId,
                    candidateId);

            if (result == null)
            {
                return NotFound(new
                {
                    Message = "Candidate not found."
                });
            }

            return Ok(result);
        }

        // =====================================================
        // Filter Options
        // =====================================================

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var result =
                await _service.GetFilterOptionsAsync();

            return Ok(result);
        }

        // =====================================================
        // Unlocked Candidates
        // =====================================================

        [HttpGet("unlocked")]
        public async Task<IActionResult> GetUnlockedCandidates(
            [FromQuery] Guid employerId)
        {
            var result =
                await _service.GetUnlockedCandidatesAsync(
                    employerId);

            return Ok(result);
        }
    }
}