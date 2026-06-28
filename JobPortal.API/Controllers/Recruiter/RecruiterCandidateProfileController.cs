using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

/// <summary>
/// Recruiter-facing full candidate profile (the "View Profile" page in the
/// employer portal). Backs:
///   GET /api/recruiter/candidates/{candidateId}/full-profile?employerId=...&jobId=...
///   GET /api/recruiter/candidates/{candidateId}/unlock-status?employerId=...
/// </summary>
[ApiController]
[Route("api/recruiter/candidates")]
[Produces("application/json")]
public class RecruiterCandidateProfileController : ControllerBase
{
    private readonly IRecruiterCandidateProfileService _service;

    public RecruiterCandidateProfileController(
        IRecruiterCandidateProfileService service)
    {
        _service = service;
    }

    [HttpGet("{candidateId:guid}/full-profile")]
    public async Task<IActionResult> GetFullProfile(
        Guid candidateId,
        [FromQuery] Guid employerId,
        [FromQuery] Guid? jobId = null)
    {
        if (employerId == Guid.Empty)
            return BadRequest(new { message = "employerId is required." });

        var result = await _service.GetFullProfileAsync(employerId, candidateId, jobId);

        if (result == null)
            return NotFound(new { message = "Candidate profile not found." });

        return Ok(result);
    }

    [HttpGet("{candidateId:guid}/unlock-status")]
    public async Task<IActionResult> GetUnlockStatus(
        Guid candidateId,
        [FromQuery] Guid employerId)
    {
        if (employerId == Guid.Empty)
            return BadRequest(new { message = "employerId is required." });

        var result = await _service.GetUnlockStatusAsync(employerId, candidateId);
        return Ok(result);
    }
}