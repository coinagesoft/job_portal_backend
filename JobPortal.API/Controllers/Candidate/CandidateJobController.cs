
using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.Implement.Candidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/jobs")]
[Produces("application/json")]
[Authorize(Roles = "Candidate")]

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
        if (queryParam.HasValue && queryParam != Guid.Empty)
            return queryParam.Value;

        var claim = User.FindFirstValue("CandidateId");

        return Guid.TryParse(claim, out var id)
            ? id
            : Guid.Empty;
    }


  


    //[HttpGet("company_details/{employerId}")]
    //public async Task<IActionResult> GetCompanyDetail(
    //Guid employerId)
    //{
    //    var result =
    //        await _jobService
    //            .GetCompanyDetailAsync(employerId);

    //    if (result == null)
    //        return NotFound(new
    //        {
    //            Success = false,
    //            Message = "Company not found."
    //        });

    //    return Ok(result);
    //}
 


    [HttpGet("filter-options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JobFilterOptionsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _jobService.GetFilterOptionsAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

  


    [HttpGet("saved")]
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

 
    [HttpGet("GetAppliedJobs")]
    [ProducesResponseType(typeof(MyApplicationsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAppliedJobs([FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to view your applications." });

        var result = await _jobService.GetMyApplicationsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }



    [HttpPost("{jobId:guid}/save")]
    [ProducesResponseType(typeof(SaveJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToggleSaveJob(
        Guid jobId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);

        if (id == Guid.Empty)
        {
            return Unauthorized(new
            {
                message = "Please log in to save jobs."
            });
        }

        var result = await _jobService.ToggleSaveJobAsync(jobId, id);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }


    [HttpGet("{jobId:guid}/questions_apply_jobs")]
    public async Task<IActionResult> GetApplyJobDetails(Guid jobId)
    {
        var candidateId = ResolveCandidateId();

        var result =
            await _jobService.GetApplyJobDetailsAsync(
                jobId,
                candidateId);

        return Ok(result);
    }




    [HttpPost("{jobId:guid}/apply")]
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


    [HttpGet("{jobId:guid}/similar")]
    public async Task<IActionResult> GetSimilarJobs(
    Guid jobId,
    [FromQuery] Guid? candidateId = null)
    {
        var result = await _jobService.GetSimilarJobsAsync(jobId, candidateId);

        return Ok(new
        {
            success = true,
            message = "Similar jobs retrieved successfully.",
            data = result
        });
    }


    [HttpDelete("applications/{applicationId:guid}")]
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