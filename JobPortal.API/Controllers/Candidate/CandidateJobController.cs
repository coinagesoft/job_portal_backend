// ============================================================
//  JobPortal.API/Controllers/Candidate/CandidateJobController.cs
//
//  Base route : api/candidate/jobs
//
//  Endpoints:
//    GET    api/candidate/jobs                          → job list + search + filters
//    GET    api/candidate/jobs/{jobId}                  → job detail
//    GET    api/candidate/jobs/filter-options           → sidebar dropdown values
//    POST   api/candidate/jobs/{jobId}/save             → toggle save/unsave (auth required)
// ============================================================

using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/jobs")]
[Produces("application/json")]
public class CandidateJobController : ControllerBase
{
    private readonly ICandidateJobService _jobService;
    private readonly ILogger<CandidateJobController> _logger;

    public CandidateJobController(
        ICandidateJobService jobService,
        ILogger<CandidateJobController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    // ── Resolve candidate ID from JWT (or query param for dev) ─
    private Guid GetCandidateId([FromQuery] Guid? candidateId = null)
    {
        if (candidateId.HasValue && candidateId != Guid.Empty)
            return candidateId.Value;

        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs
    // ────────────────────────────────────────────────────────
    // Returns paginated, filtered, and sorted active job cards.
    //
    // Query params (all optional):
    //   keyword, location, state, locationType,
    //   tradeCategory, role, experienceYearsMin, experienceYearsMax,
    //   salaryMin, salaryMax, salaryCurrency,
    //   gender, educationLevel, disabilityEligible, passportRequired,
    //   employmentType, jobType, postedWithinDays,
    //   page (default 1), pageSize (default 12, max 50),
    //   sort (newest | oldest | salary_high | salary_low)
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Job listing page — search, filter, sort, and paginate active jobs.
    /// All query parameters are optional. Accessible without login.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateJobListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs([FromQuery] CandidateJobSearchRequestDto request)
    {
        var result = await _jobService.GetJobsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs/filter-options
    // ────────────────────────────────────────────────────────
    // NOTE: must be declared BEFORE the {jobId} route below
    //       so "/filter-options" is not swallowed as a jobId.
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Returns dynamic filter option values (trade categories, cities, states, etc.)
    /// derived from currently active job postings.
    /// Use to populate sidebar dropdowns / checkboxes on the jobs-list page.
    /// </summary>
    [HttpGet("filter-options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JobFilterOptionsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _jobService.GetFilterOptionsAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs/{jobId}
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Job detail page — returns full job info including company profile,
    /// full description, eligibility, screening questions, and similar jobs.
    /// Accessible without login.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateJobDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobDetail(Guid jobId)
    {
        var result = await _jobService.GetJobDetailAsync(jobId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    // ════════════════════════════════════════════════════════
    // POST  api/candidate/jobs/{jobId}/save
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Toggle saved/bookmarked state of a job for the authenticated candidate.
    /// Returns <c>isSaved: true</c> when saved, <c>isSaved: false</c> when removed.
    ///
    /// During development pass <c>?candidateId=&lt;guid&gt;</c> as a query param
    /// to bypass JWT. In production, candidate ID is resolved from the JWT token.
    /// </summary>
    [HttpPost("{jobId:guid}/save")]
    // [Authorize(Roles = "Candidate")]    // Uncomment once JWT auth middleware is wired up
    [AllowAnonymous]                       // ← Remove once auth is enabled
    [ProducesResponseType(typeof(SaveJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToggleSaveJob(
        Guid jobId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = GetCandidateId(candidateId);

        if (id == Guid.Empty)
            return Unauthorized(new { message = "Candidate identity could not be resolved. Please log in." });

        var result = await _jobService.ToggleSaveJobAsync(jobId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}