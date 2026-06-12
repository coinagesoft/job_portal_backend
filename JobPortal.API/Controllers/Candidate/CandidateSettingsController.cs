// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateSettingsController.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;
using JobPortal.Application.DTOs.Recruiter.SupportTicket;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
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
    // PROFILE PREFERENCES — Settings main page
    // GET  /api/candidate/settings/preferences
    // PUT  /api/candidate/settings/preferences
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns the candidate's profile preferences:
    /// language, timezone, resume visibility, communication preference,
    /// 2FA status, last login, and plan name.
    /// </summary>
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

    /// <summary>
    /// Updates the candidate's profile preferences.
    /// Fields: PreferredLanguage, TimeZone, ResumeVisibility, CommunicationPreference.
    /// </summary>
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

    /// <summary>
    /// Returns the candidate's 5 notification toggles and how many are enabled.
    /// (JobMatches, ApplicationUpdates, RecruiterMessages, DocumentReminders, OffersAnnouncements)
    /// </summary>
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

    /// <summary>
    /// Saves notification toggles for the candidate.
    /// Send all 5 boolean fields; omitted fields default to false.
    /// </summary>
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

    /// <summary>
    /// Resets all notification toggles to default (all ON).
    /// </summary>
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
    // POST /api/candidate/settings/support/tickets
    // GET  /api/candidate/settings/support/tickets
    // GET  /api/candidate/settings/support/tickets/{ticketId}
    // ════════════════════════════════════════════════

    /// <summary>
    /// Raises a new support ticket for the candidate.
    /// Fields: Subject, Category, Description.
    /// Category values: ProfileResume | JobApplication | PaymentBilling |
    ///                  AccountAccess | TechnicalIssue | Other
    /// </summary>
    [HttpPost("support/tickets")]
    [ProducesResponseType(typeof(CreateSupportTicketResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateSupportTicketRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.CreateTicketAsync(id, request);

        if (!result.Success)
            return BadRequest(result);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Returns all support tickets raised by this candidate, newest first.
    /// </summary>
    [HttpGet("support/tickets")]
    [ProducesResponseType(typeof(SupportTicketListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTickets([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetTicketsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    [HttpGet("support/tickets/{ticketId:guid}/thread")]
    public async Task<IActionResult> GetTicketThread(
    Guid ticketId,
    [FromQuery] Guid candidateId)
    {
        var result = await _settingsService.GetTicketThreadAsync(
            candidateId,
            ticketId);

        return Ok(result);
    }

    [HttpPost("support/tickets/{ticketId:guid}/reply")]
    public async Task<IActionResult> AddReply(
        Guid ticketId,
        [FromQuery] Guid candidateId,
        [FromBody] AddTicketReplyRequestDto request)
    {
        var result = await _settingsService.AddReplyAsync(
            candidateId,
            ticketId,
            request);

        return Ok(result);
    }

    [HttpGet("support/summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] Guid candidateId)
    {
        var result = await _settingsService.GetSummaryAsync(candidateId);

        return Ok(result);
    }
    /// <summary>
    /// Returns a single support ticket by ticketId (must belong to the authenticated candidate).
    /// </summary>
    [HttpGet("support/tickets/{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTicketById(
        Guid ticketId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _service.GetTicketByIdAsync(id, ticketId);
        return result.Success ? Ok(result) : NotFound(result);
    }
    // GET /api/candidate/settings/support/tickets/{ticketId}/thread
    [HttpGet("support/tickets/{ticketId:guid}/thread")]
    public async Task<IActionResult> GetTicketThread(Guid ticketId, [FromQuery] Guid? candidateId = null)

// POST /api/candidate/settings/support/tickets/{ticketId}/reply
[HttpPost("support/tickets/{ticketId:guid}/reply")]
    public async Task<IActionResult> AddReply(Guid ticketId, [FromBody] AddTicketReplyRequestDto request, [FromQuery] Guid? candidateId = null)

// GET /api/candidate/settings/support/summary
[HttpGet("support/summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? candidateId = null)
}
