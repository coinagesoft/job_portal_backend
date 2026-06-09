// ============================================================
//  JobPortal.API/Controllers/Candidate/CandidateJobController.cs
//
//  Base route : api/candidate/jobs
//
//  Endpoints:
//    GET    api/candidate/jobs                          → job list + search + filters
//    GET    api/candidate/jobs/filter-options           → sidebar dropdown values
//    GET    api/candidate/jobs/{jobId}                  → job detail
//    POST   api/candidate/jobs/{jobId}/save             → toggle save/unsave
//
//    GET    api/candidate/jobs/saved                    → saved jobs list
//    POST   api/candidate/jobs/{jobId}/apply            → apply now (with screening answers)
//
//    GET    api/candidate/jobs/my-applications          → my application history
//    DELETE api/candidate/jobs/applications/{appId}     → withdraw application
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

    // ── Resolve candidate ID from JWT or dev query param ─────
    private Guid ResolveCandidateId(Guid? queryParam = null)
    {
        if (queryParam.HasValue && queryParam != Guid.Empty) return queryParam.Value;
        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Jobs-list page — paginated, filtered, sorted active job cards.
    /// All query params optional. Public (no auth needed).
    /// Filters: keyword, location, state, locationType, tradeCategory, role,
    /// experienceYearsMin/Max, salaryMin/Max, salaryCurrency, gender, educationLevel,
    /// disabilityEligible, passportRequired, employmentType, jobType, postedWithinDays,
    /// page (default 1), pageSize (default 12, max 50),
    /// sort: newest | oldest | salary_high | salary_low
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
    // NOTE: declared before {jobId} so the literal isn't treated as a Guid
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Returns dynamic filter values (cities, trades, roles, etc.) from live jobs.
    /// Used to populate sidebar dropdowns / checkboxes on the jobs-list page.
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
    // GET  api/candidate/jobs/saved
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Returns all bookmarked jobs for the candidate — exactly the Saved Jobs page.
    /// Each card shows: company, job title, location, salary, employment type,
    /// experience, short description, tags, application deadline,
    /// and whether the candidate has already applied.
    ///
    /// Pass ?candidateId= during development (remove once JWT is active).
    /// </summary>
    [HttpGet("saved")]
    [AllowAnonymous]                        // ← swap to [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(SavedJobListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedJobs([FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to view saved jobs." });

        var result = await _jobService.GetSavedJobsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs/my-applications
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Returns all job applications submitted by the candidate, newest first.
    /// Includes: job info, company info, application status, applied time, withdrawal flag.
    ///
    /// Application statuses: Applied | Viewed | Shortlisted | Interview | Rejected | Hired | Withdrawn
    /// </summary>
    [HttpGet("my-applications")]
    [AllowAnonymous]                        // ← [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(MyApplicationsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyApplications([FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to view your applications." });

        var result = await _jobService.GetMyApplicationsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // GET  api/candidate/jobs/{jobId}
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Job details page — full job info, company profile, eligibility, screening
    /// questions, and similar jobs. Public (no auth needed).
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateJobDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobDetail(Guid jobId)
    {
        var result = await _jobService.GetJobDetailAsync(jobId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ════════════════════════════════════════════════════════
    // POST  api/candidate/jobs/{jobId}/save
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Toggle save/bookmark state for a job.
    /// Returns isSaved: true when bookmarked, isSaved: false when removed.
    /// </summary>
    [HttpPost("{jobId:guid}/save")]
    [AllowAnonymous]                        // ← [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(SaveJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToggleSaveJob(
        Guid jobId, [FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to save jobs." });

        var result = await _jobService.ToggleSaveJobAsync(jobId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // POST  api/candidate/jobs/{jobId}/apply
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Apply Now — submits a job application.
    ///
    /// Request body (JSON):
    /// {
    ///   "passportGatePassed": true,          // required when job.passportRequired = true
    ///   "screeningAnswers": [
    ///     { "questionIndex": 0, "answer": "Yes" },
    ///     { "questionIndex": 1, "answer": "5 years on cargo vessels" },
    ///     { "questionIndex": 2, "answer": "Yes" }
    ///   ]
    /// }
    ///
    /// Validates:
    ///   ✓ Job is Active and deadline not passed
    ///   ✓ Candidate profile exists and is Active
    ///   ✓ Not already applied to this job
    ///   ✓ Passport gate (if job requires passport)
    ///   ✓ All mandatory screening questions answered
    ///
    /// On success: creates JobApplication, increments job.AppliedCount,
    /// updates candidate.LastAppliedAt.
    /// </summary>
    [HttpPost("{jobId:guid}/apply")]
    [AllowAnonymous]                        // ← [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(ApplyJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyJob(
        Guid jobId,
        [FromBody] ApplyJobRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to apply for jobs." });

        var result = await _jobService.ApplyJobAsync(jobId, id, request);

        // Return 409 specifically for "already applied" to let the UI handle it differently
        if (!result.Success && result.Message.Contains("already applied"))
            return Conflict(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════════════
    // DELETE  api/candidate/jobs/applications/{applicationId}
    // ════════════════════════════════════════════════════════
    /// <summary>
    /// Withdraw a submitted application.
    /// Only allowed when: withdrawalAllowed = true AND status is not Hired or Rejected.
    /// Sets status to "Withdrawn" and decrements job's applied count.
    /// </summary>
    [HttpDelete("applications/{applicationId:guid}")]
    [AllowAnonymous]                        // ← [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(WithdrawApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> WithdrawApplication(
        Guid applicationId, [FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to manage applications." });

        var result = await _jobService.WithdrawApplicationAsync(applicationId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}