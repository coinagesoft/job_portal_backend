

using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

[ApiController]
[Route("api/recruiter/auth")]
[Produces("application/json")]
public class RecruiterAuthController : ControllerBase
{
    private readonly IRecruiterAuthService _service;
    private readonly ILogger<RecruiterAuthController> _logger;

    public RecruiterAuthController(
        IRecruiterAuthService service,
        ILogger<RecruiterAuthController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ── SEND OTP ──────────────────────────────────────
    /// <summary>
    /// Step 1 — Send OTP to email or mobile.
    /// Works for both Candidate and Employer.
    /// </summary>
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(SendOtpResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrors());

            var result = await _service.SendOtpAsync(request, GetIp());

            if (!result.Success)
            {
                return result.Message switch
                {
                    var m when m.Contains("not found") ||
                               m.Contains("No") => NotFound(result),
                    var m when m.Contains("suspended") => StatusCode(403, result),
                    var m when m.Contains("wait") => StatusCode(429, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendOtp controller error.");
            return StatusCode(500, Error("An error occurred."));
        }
    }

    // ── VERIFY OTP ────────────────────────────────────
    /// <summary>
    /// Step 2 — Verify OTP and receive JWT token.
    /// </summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrors());

            var result = await _service.VerifyOtpAsync(request, GetIp());

            if (!result.Success)
            {
                return result.Message switch
                {
                    var m when m.Contains("not found") => NotFound(result),
                    var m when m.Contains("expired") => BadRequest(result),
                    var m when m.Contains("Invalid") ||
                               m.Contains("attempts") => Unauthorized(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyOtp controller error.");
            return StatusCode(500, Error("An error occurred."));
        }
    }

    // ── GOOGLE LOGIN ──────────────────────────────────
    /// <summary>
    /// Login or register with Google.
    /// Candidates are auto-registered. Employers must register first.
    /// </summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrors());

            var result = await _service.GoogleLoginAsync(request, GetIp());

            if (!result.Success)
            {
                return result.Message switch
                {
                    var m when m.Contains("suspended") => StatusCode(403, result),
                    var m when m.Contains("Invalid Google") => Unauthorized(result),
                    var m when m.Contains("register") => NotFound(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleLogin controller error.");
            return StatusCode(500, Error("An error occurred."));
        }
    }

    // ── LINKEDIN LOGIN ────────────────────────────────
    /// <summary>
    /// Login or register with LinkedIn.
    /// Pass the authorization code from LinkedIn OAuth redirect.
    /// </summary>
    [HttpPost("linkedin")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LinkedInLogin(
        [FromBody] LinkedInLoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ValidationErrors());

            var result = await _service.LinkedInLoginAsync(request, GetIp());

            if (!result.Success)
            {
                return result.Message switch
                {
                    var m when m.Contains("suspended") => StatusCode(403, result),
                    var m when m.Contains("failed") => Unauthorized(result),
                    var m when m.Contains("register") => NotFound(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LinkedInLogin controller error.");
            return StatusCode(500, Error("An error occurred."));
        }
    }

    // ── Helpers ───────────────────────────────────────
    private object ValidationErrors() => new
    {
        success = false,
        message = "Validation failed.",
        errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                k => k.Key,
                v => v.Value!.Errors
                    .Select(e => e.ErrorMessage).ToList())
    };

    private static object Error(string message) =>
        new { success = false, message };
}
