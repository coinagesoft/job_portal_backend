// ============================================================
//  JobPortal.API/Controllers/Candidate/ApplicationStatusController.cs
//  Base route: api/candidate/applications
// ============================================================

using JobPortal.Application.DTOs.Candidate.Applications;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/applications")]
[Produces("application/json")]
public class ApplicationStatusController : ControllerBase
{
    private readonly IApplicationStatusService _statusService;
    private readonly ILogger<ApplicationStatusController> _logger;

    public ApplicationStatusController(
        IApplicationStatusService statusService,
        ILogger<ApplicationStatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    private Guid ResolveCandidateId(Guid? queryParam = null)
    {
        if (queryParam.HasValue && queryParam != Guid.Empty) return queryParam.Value;
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// GET api/candidate/applications/status
    /// Returns stats bar, filter tab counts, and all application cards with recruiter notes.
    /// Query: ?candidateId= (dev) | ?status=Applied|InReview|Shortlisted|Interview|Rejected
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous] // ← swap to [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(ApplicationStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetApplicationStatus(
        [FromQuery] Guid? candidateId = null,
        [FromQuery] string? status = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to view your application status." });

        var result = await _statusService.GetApplicationStatusAsync(id, new ApplicationStatusFilterDto { Status = status });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST api/candidate/applications/{applicationId}/acknowledge-note
    /// Candidate acknowledges the recruiter note. Badge flips "Awaiting" → "Acknowledged".
    /// </summary>
    [HttpPost("{applicationId:guid}/acknowledge-note")]
    [AllowAnonymous] // ← [Authorize(Roles = "Candidate")] in prod
    [ProducesResponseType(typeof(AcknowledgeNoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeRecruiterNote(
        Guid applicationId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = ResolveCandidateId(candidateId);
        if (id == Guid.Empty)
            return Unauthorized(new { message = "Please log in to acknowledge this message." });

        var result = await _statusService.AcknowledgeRecruiterNoteAsync(applicationId, id);

        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }
}