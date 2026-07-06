using JobPortal.Application.DTOs.Recruiter.Notification;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/notification")]
public class CandidateNotificationController : ControllerBase
{
    private readonly ICandidateNotificationService _service;

    public CandidateNotificationController(
        ICandidateNotificationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get Notifications  (filter = "all" | "unread")
    /// </summary>
    [HttpGet("notifications/{candidateId:guid}")]
    public async Task<IActionResult> GetNotifications(
        Guid candidateId,
        [FromQuery] string filter = "all")
    {
        var result = await _service
            .GetNotificationsAsync(
                candidateId,
                filter);

        return Ok(result);
    }

    /// <summary>
    /// Mark Single Notification As Read
    /// </summary>
    [HttpPatch("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId)
    {
        var result = await _service
            .MarkNotificationAsReadAsync(
                notificationId);

        if (!result)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Mark All Notifications As Read
    /// </summary>
    [HttpPatch("notifications/{candidateId:guid}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        Guid candidateId)
    {
        await _service
            .MarkAllNotificationsAsReadAsync(
                candidateId);

        return Ok();
    }

    /// <summary>
    /// Get Notification Settings
    /// </summary>
    [HttpGet("notification-settings/{candidateId:guid}")]
    public async Task<IActionResult> GetSettings(
        Guid candidateId)
    {
        var result = await _service
            .GetNotificationSettingsAsync(
                candidateId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Update Notification Settings
    /// </summary>
    [HttpPatch("notification-settings/{candidateId:guid}")]
    public async Task<IActionResult> UpdateSettings(
        Guid candidateId,
        [FromBody] UpdateNotificationSettingsDto request)
    {
        var result = await _service
            .UpdateNotificationSettingsAsync(
                candidateId,
                request);

        if (!result)
            return NotFound();

        return Ok(new
        {
            Success = true,
            Message = "Notification settings updated successfully."
        });
    }
}