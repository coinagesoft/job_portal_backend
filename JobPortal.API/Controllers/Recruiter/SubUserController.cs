using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Application.DTOs.SubUser;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Recruiter;

[ApiController]
[Route("api/recruiter/sub-users")]
[Produces("application/json")]
[Authorize(Roles = "Recruiter")]
public class RecruiterSubUserController : ControllerBase
{
    private readonly ISubUserService _service;
    private readonly ILogger<RecruiterSubUserController> _logger;

    public RecruiterSubUserController(
        ISubUserService service,
        ILogger<RecruiterSubUserController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Hardcoded for testing — replace with JWT claim ─
    private Guid GetEmployerId()
    {
        var employerId = User.FindFirst("EmployerId")?.Value;

        if (string.IsNullOrWhiteSpace(employerId))
            throw new UnauthorizedAccessException(
                "Employer ID not found in token.");

        return Guid.Parse(employerId);
    }

    private Guid GetActionUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ════════════════════════════════════════════════
    // GET MY PERMISSIONS — called on login and on every
    // page refresh so the frontend can hide/block restricted
    // pages using the caller's actual, current flags.
    // ════════════════════════════════════════════════
    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        try
        {
            var result = await _service.GetMyPermissionsAsync(
                GetActionUserId(), GetEmployerId());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyPermissions failed.");
            return StatusCode(500, Error("Failed to retrieve permissions."));
        }
    }

    // ════════════════════════════════════════════════
    // GET ALL SUB-USERS
    // ════════════════════════════════════════════════
    /// <summary>Get all sub-users for the employer account.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SubUserListResponseDto), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSubUsers()
    {
        try
        {
            var employerId = GetEmployerId();
            var result = await _service.GetSubUsersAsync(employerId);

            return result.Success
                ? Ok(result)
                : StatusCode(500, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSubUsers failed.");
            return StatusCode(500, Error("Failed to retrieve sub-users."));
        }
    }

    // ════════════════════════════════════════════════
    // GET ROLE PERMISSIONS
    // ════════════════════════════════════════════════
    /// <summary>
    /// Get permission matrix for a role.
    /// Used to populate the Permission Matrix panel in UI.
    /// </summary>
    [HttpGet("role-permissions/{role}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult GetRolePermissions(SubUserRole role)
    {
        try
        {
            // ── Validate enum value ────────────────────────
            if (!Enum.IsDefined(typeof(SubUserRole), role))
                return BadRequest(Error(
                    $"Invalid role. Valid values: {string.Join(", ", Enum.GetNames<SubUserRole>())}"));

            var permissions = _service.GetRolePermissions(role);
            return Ok(new
            {
                success = true,
                role = role.ToString(),
                permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRolePermissions failed for role {Role}.", role);
            return StatusCode(500, Error("Failed to get permissions."));
        }
    }

    // ════════════════════════════════════════════════
    // INVITE SUB-USER
    // ════════════════════════════════════════════════
    /// <summary>
    /// Invite a new sub-user. Sends an invite link valid for 72 hours.
    /// </summary>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(InviteSubUserResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> InviteSubUser(
        [FromBody] InviteSubUserRequestDto request)
    {
        try
        {
            // ── Validate model ─────────────────────────────
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            k => k.Key,
                            v => v.Value!.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList())
                });

            // ── Validate mobile number format ──────────────
            if (!IsValidMobile(request.SubUserMobile))
                return BadRequest(Error("Mobile number must be 7–12 digits."));

            // ── Validate email format ──────────────────────
            if (!IsValidEmail(request.SubUserEmail))
                return BadRequest(Error("Invalid email format."));

            var employerId = GetEmployerId();
            var result = await _service.InviteSubUserAsync(request, employerId);

            if (!result.Success)
            {
                // Conflict — email already exists
                if (result.Message.Contains("already"))
                    return Conflict(result);

                // Limit reached
                if (result.Message.Contains("Maximum"))
                    return UnprocessableEntity(result);

                return BadRequest(result);
            }

            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "InviteSubUser failed for email {Email}.",
                request?.SubUserEmail);
            return StatusCode(500, Error("Failed to send invite. Please try again."));
        }
    }

    // ════════════════════════════════════════════════
    // UPDATE SUB-USER
    // ════════════════════════════════════════════════
    /// <summary>Update role and permissions of an existing sub-user.</summary>
    [HttpPut("{subUserId:guid}")]
    [ProducesResponseType(typeof(InviteSubUserResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateSubUser(
        Guid subUserId,
        [FromBody] UpdateSubUserRequestDto request)
    {
        try
        {
            // ── Validate model ─────────────────────────────
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            k => k.Key,
                            v => v.Value!.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList())
                });

            // ── Validate subUserId ─────────────────────────
            if (subUserId == Guid.Empty)
                return BadRequest(Error("Invalid sub-user ID."));

            var employerId = GetEmployerId();
            var result = await _service.UpdateSubUserAsync(
                subUserId, request, employerId);

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                if (result.Message.Contains("deactivated"))
                    return UnprocessableEntity(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateSubUser failed for SubUserId {SubUserId}.", subUserId);
            return StatusCode(500, Error("Failed to update sub-user."));
        }
    }

    // ════════════════════════════════════════════════
    // DEACTIVATE
    // ════════════════════════════════════════════════
    /// <summary>
    /// Deactivate a sub-user. Revokes login access immediately.
    /// Audit logs are kept intact.
    /// </summary>
    [HttpPut("{subUserId:guid}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Deactivate(Guid subUserId)
    {
        try
        {
            if (subUserId == Guid.Empty)
                return BadRequest(Error("Invalid sub-user ID."));

            var result = await _service.DeactivateSubUserAsync(
                subUserId, GetEmployerId());

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                if (result.Message.Contains("already deactivated"))
                    return Conflict(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Deactivate failed for SubUserId {SubUserId}.", subUserId);
            return StatusCode(500, Error("Failed to deactivate sub-user."));
        }
    }

    // ════════════════════════════════════════════════
    // REACTIVATE
    // ════════════════════════════════════════════════
    /// <summary>Reactivate a previously deactivated sub-user.</summary>
    [HttpPut("{subUserId:guid}/reactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Reactivate(Guid subUserId)
    {
        try
        {
            if (subUserId == Guid.Empty)
                return BadRequest(Error("Invalid sub-user ID."));

            var result = await _service.ReactivateSubUserAsync(
                subUserId, GetEmployerId());

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Reactivate failed for SubUserId {SubUserId}.", subUserId);
            return StatusCode(500, Error("Failed to reactivate sub-user."));
        }
    }

    // ════════════════════════════════════════════════
    // RESEND INVITE
    // ════════════════════════════════════════════════
    /// <summary>
    /// Resend invite email to a pending sub-user.
    /// Generates a new token and resets 72-hour expiry.
    /// </summary>
    [HttpPost("{subUserId:guid}/resend-invite")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ResendInvite(Guid subUserId)
    {
        try
        {
            if (subUserId == Guid.Empty)
                return BadRequest(Error("Invalid sub-user ID."));

            var result = await _service.ResendInviteAsync(
                subUserId, GetEmployerId());

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                // Already accepted — no point resending
                if (result.Message.Contains("already accepted"))
                    return Conflict(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ResendInvite failed for SubUserId {SubUserId}.", subUserId);
            return StatusCode(500, Error("Failed to resend invite."));
        }
    }

    // ════════════════════════════════════════════════
    // ACCEPT INVITE
    // ════════════════════════════════════════════════
    /// <summary>
    /// Accept an invite via token from email link.
    /// Called by the sub-user when they click the invite link.
    /// </summary>
    /// [AllowAnonymous]
    /// 
    [AllowAnonymous]
    [HttpPost("accept-invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(Error("Request is required."));
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(Error("Invite token is required."));
            }

            if (!Guid.TryParse(request.Token, out _))
            {
                return BadRequest(Error("Invalid invite token."));
            }




            var result = await _service.AcceptInviteAsync(request);

            if (!result.Success)
            {
                if (result.Message.Contains("expired", StringComparison.OrdinalIgnoreCase))
                    return StatusCode(StatusCodes.Status410Gone, result);

                if (result.Message.Contains("already accepted", StringComparison.OrdinalIgnoreCase))
                    return Conflict(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AcceptInvite failed for token {Token}.",
                request?.Token);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Error("Failed to accept invitation."));
        }
    }



    [HttpDelete("{subUserId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteSubUser(Guid subUserId)
    {
        try
        {
            if (subUserId == Guid.Empty)
                return BadRequest(Error("Invalid sub-user ID."));

            var result = await _service.DeleteSubUserAsync(
                subUserId,
                GetEmployerId());

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DeleteSubUser failed for {SubUserId}",
                subUserId);

            return StatusCode(500,
                Error("Failed to delete sub-user."));
        }
    }
    // ── Private Helpers ───────────────────────────────────



    [AllowAnonymous]
    [HttpGet("validate-invite/{token}")]
    public async Task<IActionResult> ValidateInvite(string token)
    {
        var result = await _service.ValidateInviteAsync(token);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>Standard error response shape</summary>
    private static object Error(string message) =>
        new { success = false, message };

    /// <summary>Basic mobile number validation</summary>
    private static bool IsValidMobile(string mobile) =>
        !string.IsNullOrWhiteSpace(mobile) &&
        mobile.All(char.IsDigit) &&
        mobile.Length >= 7 &&
        mobile.Length <= 12;

    /// <summary>Basic email validation</summary>
    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Contains('@') &&
        email.Contains('.');
}