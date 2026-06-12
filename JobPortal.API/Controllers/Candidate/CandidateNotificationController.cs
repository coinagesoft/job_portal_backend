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

    [HttpGet("notifications/{candidateId:guid}")]
    public async Task<IActionResult> GetNotifications(
        Guid candidateId,
        [FromQuery] string filter = "all")
    {
        var result =
            await _service.GetNotificationsAsync(
                candidateId,
                filter);

        return Ok(result);
    }

    [HttpPatch("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId)
    {
        var result =
            await _service.MarkNotificationAsReadAsync(
                notificationId);

        return Ok(result);
    }

    [HttpPatch("notifications/{candidateId:guid}/read-all")]
    public async Task<IActionResult> MarkAllRead(
        Guid candidateId)
    {
        var result =
            await _service.MarkAllNotificationsAsReadAsync(
                candidateId);

        return Ok(result);
    }
}