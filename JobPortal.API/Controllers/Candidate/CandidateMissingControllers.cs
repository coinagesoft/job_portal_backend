using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.Implement.Candidate;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

// ════════════════════════════════════════════════════════════════
// AVAILABILITY CONTROLLER
// GET  /api/candidate/profile/availability
// PUT  /api/candidate/profile/availability
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/candidate/profile")]
[Produces("application/json")]
public class CandidateAvailabilityController : ControllerBase
{
    private readonly ICandidateAvailabilityService _service;
    public CandidateAvailabilityController(ICandidateAvailabilityService service) => _service = service;

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet("availability")]
    [ProducesResponseType(typeof(AvailabilityResponseDto), 200)]
    public async Task<IActionResult> GetAvailability([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.GetAvailabilityAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("availability")]
    [ProducesResponseType(typeof(AvailabilityResponseDto), 200)]
    public async Task<IActionResult> UpdateAvailability(
        [FromBody] UpdateAvailabilityRequestDto request, [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.UpdateAvailabilityAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}


// ════════════════════════════════════════════════════════════════
// ITI INFO CONTROLLER
// GET  /api/candidate/profile/iti-info
// PUT  /api/candidate/profile/iti-info
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/candidate/profile")]
[Produces("application/json")]
public class CandidateItiInfoController : ControllerBase
{
    private readonly ICandidateItiInfoService _service;
    public CandidateItiInfoController(ICandidateItiInfoService service) => _service = service;

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet("iti-info")]
    [ProducesResponseType(typeof(ItiInfoResponseDto), 200)]
    public async Task<IActionResult> GetItiInfo([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.GetItiInfoAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("iti-info")]
    [ProducesResponseType(typeof(UpdateItiInfoResponseDto), 200)]
    public async Task<IActionResult> UpdateItiInfo(
        [FromBody] UpdateItiInfoRequestDto request, [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.UpdateItiInfoAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}


// ════════════════════════════════════════════════════════════════
// LOGOUT CONTROLLER
// POST /api/candidate/auth/logout
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/candidate/auth")]
[Produces("application/json")]
public class CandidateAuthExtController : ControllerBase
{
    private readonly ICandidateLogoutService _logoutService;
    public CandidateAuthExtController(ICandidateLogoutService logoutService) => _logoutService = logoutService;

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(CandidateLogoutResponseDto), 200)]
    public async Task<IActionResult> Logout([FromBody] CandidateLogoutRequestDto request)
    {
        var id = GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });

        var jwtJti = User.FindFirstValue("jti");
        var expClaim = User.FindFirstValue("exp");
        DateTime? expiry = null;
        if (long.TryParse(expClaim, out var exp))
            expiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

        var result = await _logoutService.LogoutAsync(id, request, jwtJti, expiry);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}


// ════════════════════════════════════════════════════════════════
// PAGED JOB CONTROLLER
// GET /api/candidate/jobs/saved/paged
// GET /api/candidate/applications/status/paged
// ════════════════════════════════════════════════════════════════
[ApiController]
[Produces("application/json")]
public class CandidatePagedJobController : ControllerBase
{
    private readonly CandidatePagedJobService _service;
    public CandidatePagedJobController(CandidatePagedJobService service) => _service = service;

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet("api/candidate/jobs/saved/paged")]
    [ProducesResponseType(typeof(PagedSavedJobListResponseDto), 200)]
    public async Task<IActionResult> GetPagedSavedJobs(
        [FromQuery] PagedSavedJobRequestDto request, [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.GetPagedSavedJobsAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("api/candidate/applications/status/paged")]
    [ProducesResponseType(typeof(PagedApplicationStatusResponseDto), 200)]
    public async Task<IActionResult> GetPagedApplicationStatus(
        [FromQuery] PagedApplicationStatusRequestDto request, [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty) return BadRequest(new { message = "Unable to resolve candidate identity." });
        var result = await _service.GetPagedApplicationStatusAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}