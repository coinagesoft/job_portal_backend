// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateSettingsController.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/settings")]
[Produces("application/json")]
// [Authorize(Roles = "Candidate")]   // Uncomment once JWT auth middleware is wired up
public class CandidateSettingsController : ControllerBase
{
    private readonly ICandidateSettingsService _service;
    private readonly ILogger<CandidateSettingsController> _logger;

    public CandidateSettingsController(
        ICandidateSettingsService service,
        ILogger<CandidateSettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Extracts CandidateId from JWT claim; falls back to query param for dev.</summary>
    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════
    // PROFILE PREFERENCES
    // GET  /api/candidate/settings/preferences
    // PUT  /api/candidate/settings/preferences
    // ════════════════════════════════════════════════

    /// <summary>Returns the candidate's profile preferences.</summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(CandidatePreferenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetPreferencesAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Updates the candidate's profile preferences.</summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(UpdateCandidatePreferenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateCandidatePreferenceRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdatePreferencesAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // NOTIFICATION PREFERENCES
    // GET  /api/candidate/settings/notifications
    // PUT  /api/candidate/settings/notifications
    // PUT  /api/candidate/settings/notifications/reset
    // ════════════════════════════════════════════════

    /// <summary>Returns the candidate's 5 notification toggles.</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(CandidateNotificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNotifications([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetNotificationsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Saves notification toggles for the candidate.</summary>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(CandidateNotificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateNotifications(
        [FromBody] UpdateCandidateNotificationRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.UpdateNotificationsAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Resets all notification toggles to default (all ON).</summary>
    [HttpPut("notifications/reset")]
    [ProducesResponseType(typeof(CandidateNotificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetNotifications([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.ResetNotificationsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // HELP & SUPPORT — Support Tickets
    // POST   /api/candidate/settings/support/tickets/{candidateId}
    // GET    /api/candidate/settings/support/tickets/{candidateId}
    // GET    /api/candidate/settings/support/thread/{ticketId}
    // POST   /api/candidate/settings/support/tickets/{ticketId}/reply/{candidateId}
    // PATCH  /api/candidate/settings/support/tickets/{ticketId}/resolve
    // GET    /api/candidate/settings/support/{candidateId}/summary
    // ════════════════════════════════════════════════

    /// <summary>Create Support Ticket</summary>
    [HttpPost("support/tickets/{candidateId:guid}")]
    [ProducesResponseType(typeof(CandidateCreateTicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTicket(
        Guid candidateId,
        [FromForm] CandidateCreateTicketRequestDto request)
    {
        var result = await _service.CreateTicketAsync(candidateId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>Get All Tickets</summary>
    [HttpGet("support/tickets/{candidateId:guid}")]
    [ProducesResponseType(typeof(CandidateTicketListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTickets(Guid candidateId)
    {
        var result = await _service.GetTicketsAsync(candidateId);
        return Ok(result);
    }

    /// <summary>Get Ticket Thread (with replies)</summary>
    [HttpGet("support/thread/{ticketId:guid}")]
    [ProducesResponseType(typeof(CandidateTicketThreadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketThread(
        Guid ticketId,
        [FromQuery] Guid candidateId)
    {
        var result = await _service.GetTicketThreadAsync(candidateId, ticketId);

        if (result == null)
            return NotFound(new { Success = false, Message = "Ticket not found." });

        return Ok(result);
    }

    /// <summary>Add Reply To Ticket</summary>
    [HttpPost("support/tickets/{ticketId:guid}/reply/{candidateId:guid}")]
    [ProducesResponseType(typeof(CandidateAddReplyResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddReply(
        Guid ticketId,
        Guid candidateId,
        [FromBody] CandidateAddReplyRequestDto request)
    {
        var result = await _service.AddReplyAsync(candidateId, ticketId, request);
        return Ok(result);
    }

    /// <summary>Mark Ticket Resolved</summary>
    [HttpPatch("support/tickets/{ticketId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveTicket(Guid ticketId)
    {
        var result = await _service.ResolveTicketAsync(ticketId);

        if (!result)
            return NotFound(new { Success = false, Message = "Ticket not found." });

        return Ok(new { Success = true, Message = "Ticket resolved successfully." });
    }

    /// <summary>Ticket Summary</summary>
    [HttpGet("support/{candidateId:guid}/summary")]
    [ProducesResponseType(typeof(CandidateTicketSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid candidateId)
    {
        var result = await _service.GetSummaryAsync(candidateId);
        return Ok(result);
    }
}