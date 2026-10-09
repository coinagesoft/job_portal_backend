// ============================================================
//  JobPortal.API/Controllers/Candidate/CandidateAccountController.cs
//
//  Candidate self-service account deletion (mobile -> OTP -> delete).
//
//    POST /api/candidate/account/delete/send-otp
//    POST /api/candidate/account/delete/confirm   { "otpCode": "123456" }
//
//  Both endpoints require the candidate's JWT. The candidate is always
//  taken from the token — never from the request — so a candidate can
//  only ever delete their own account.
// ============================================================

using JobPortal.Application.DTOs.Candidate.Account;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/account")]
[Produces("application/json")]
[Authorize(Roles = "Candidate")]
public class CandidateAccountController : ControllerBase
{
    private readonly ICandidateAccountService _service;
    private readonly ILogger<CandidateAccountController> _logger;

    public CandidateAccountController(
        ICandidateAccountService service,
        ILogger<CandidateAccountController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // JwtService issues this claim as "CandidateId".
    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("CandidateId");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ── STEP 1 — SEND OTP ─────────────────────────────
    /// <summary>
    /// Step 1 — sends a delete-confirmation OTP by SMS to the mobile
    /// number registered on the logged-in candidate's account.
    /// </summary>
    [HttpPost("delete/send-otp")]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendDeleteOtp()
    {
        try
        {
            var candidateId = GetCandidateId();
            if (candidateId == Guid.Empty)
                return BadRequest(new { success = false, message = "Unable to resolve candidate identity." });

            var result = await _service.SendDeleteAccountOtpAsync(candidateId, GetIp());

            if (result.Success)
                return Ok(result);

            return result.Message.Contains("wait", StringComparison.OrdinalIgnoreCase)
                ? StatusCode(StatusCodes.Status429TooManyRequests, result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate delete-account send-otp controller error.");
            return StatusCode(500, new { success = false, message = "An error occurred." });
        }
    }

    // ── STEP 2 — VERIFY OTP + DELETE ──────────────────
    /// <summary>
    /// Step 2 — verifies the OTP and, only if it is valid, permanently
    /// deletes the logged-in candidate's account.
    /// </summary>
    [HttpPost("delete/confirm")]
    [ProducesResponseType(typeof(CandidateDeleteAccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CandidateDeleteAccountResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmDelete(
        [FromBody] CandidateDeleteAccountRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var candidateId = GetCandidateId();
            if (candidateId == Guid.Empty)
                return BadRequest(new { success = false, message = "Unable to resolve candidate identity." });

            var jwtJti = User.FindFirstValue("jti");

            DateTime? expiry = null;
            if (long.TryParse(User.FindFirstValue("exp"), out var exp))
                expiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

            var result = await _service.ConfirmDeleteAccountAsync(
                candidateId, request, jwtJti, expiry, GetIp());

            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate delete-account confirm controller error.");
            return StatusCode(500, new { success = false, message = "An error occurred." });
        }
    }

    // ════════════════════════════════════════════════
    // PUBLIC FLOW — NO TOKEN REQUIRED
    // For the Play Store "account deletion" link / web page: the person
    // enters their mobile number or email, receives an OTP, and the
    // account is deleted only after the OTP is verified.
    //
    //   POST /api/candidate/account-deletion/send-otp
    //   POST /api/candidate/account-deletion/confirm
    // ════════════════════════════════════════════════

    /// <summary>
    /// Public step 1 — send a delete-confirmation OTP to a registered
    /// mobile number (SMS) or email. No login needed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("~/api/candidate/account-deletion/send-otp")]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SendOtpResponseDto), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> PublicSendDeleteOtp(
        [FromBody] CandidatePublicDeleteSendOtpRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.SendPublicDeleteOtpAsync(request, GetIp());

            if (result.Success)
                return Ok(result);

            return result.Message.Contains("wait", StringComparison.OrdinalIgnoreCase)
                ? StatusCode(StatusCodes.Status429TooManyRequests, result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public delete-account send-otp controller error.");
            return StatusCode(500, new { success = false, message = "An error occurred." });
        }
    }

    /// <summary>
    /// Public step 2 — verify the OTP and, only if valid, delete the
    /// account. No login needed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("~/api/candidate/account-deletion/confirm")]
    [ProducesResponseType(typeof(CandidateDeleteAccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CandidateDeleteAccountResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublicConfirmDelete(
        [FromBody] CandidatePublicDeleteConfirmRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.ConfirmPublicDeleteAsync(request, GetIp());

            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public delete-account confirm controller error.");
            return StatusCode(500, new { success = false, message = "An error occurred." });
        }
    }
}