using JobPortal.Application.DTOs.Candidate.Push;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

/// <summary>
/// FCM token registration for the candidate mobile app.
/// The candidate is ALWAYS resolved from the signed JWT - never from the body or URL.
/// </summary>
[ApiController]
[Route("api/candidate/device-tokens")]
[Produces("application/json")]
[Authorize(Roles = "Candidate")]
public class CandidateDeviceTokenController : ControllerBase
{
    private readonly ICandidateDeviceTokenService _tokenService;
    private readonly IApplicationStatusPushService _pushService;
    private readonly IConfiguration _configuration;

    public CandidateDeviceTokenController(
        ICandidateDeviceTokenService tokenService,
        IApplicationStatusPushService pushService,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _pushService = pushService;
        _configuration = configuration;
    }

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("CandidateId");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Register / refresh the FCM token of the signed-in candidate's device.
    /// Call after login, on app start while logged in, and on every onTokenRefresh.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeviceTokenResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenRequestDto request)
    {
        var candidateId = GetCandidateId();
        if (candidateId == Guid.Empty)
            return BadRequest(new DeviceTokenResponseDto
            {
                Success = false,
                Message = "Unable to resolve candidate identity."
            });

        var result = await _tokenService.RegisterAsync(candidateId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove this device's token (call on logout).</summary>
    [HttpPost("unregister")]
    [ProducesResponseType(typeof(DeviceTokenResponseDto), 200)]
    public async Task<IActionResult> Unregister([FromBody] UnregisterDeviceTokenRequestDto request)
    {
        var candidateId = GetCandidateId();
        if (candidateId == Guid.Empty)
            return BadRequest(new DeviceTokenResponseDto
            {
                Success = false,
                Message = "Unable to resolve candidate identity."
            });

        var result = await _tokenService.UnregisterAsync(candidateId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Sends a test push to the caller's own devices. Disabled unless
    /// "Firebase:EnableTestEndpoint" is true in configuration.
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTest(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("Firebase:EnableTestEndpoint"))
            return NotFound();

        var candidateId = GetCandidateId();
        if (candidateId == Guid.Empty)
            return BadRequest(new { success = false, message = "Unable to resolve candidate identity." });

        var delivered = await _pushService.SendTestAsync(candidateId, cancellationToken);

        return Ok(new
        {
            success = delivered > 0,
            devicesReached = delivered,
            message = delivered > 0
                ? "Test push sent."
                : "No registered device accepted the push. Register a token first."
        });
    }
}