

using JobPortal.Application.DTOs.Recruiter.Notification;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
    {
        [ApiController]
        [Route("api/recruiter")]
        public class NotificationController : ControllerBase
        {
            private readonly INotificationService _service;

            public NotificationController(
                INotificationService service)
            {
                _service = service;
            }

            [HttpGet("notifications/{employerId:guid}")]
            public async Task<IActionResult> GetNotifications(
                Guid employerId,
                string filter = "all")
            {
                var result = await _service
                    .GetNotificationsAsync(
                        employerId,
                        filter);

                return Ok(result);
            }

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

            [HttpPatch("notifications/{employerId:guid}/read-all")]
            public async Task<IActionResult> MarkAllAsRead(
                Guid employerId)
            {
                await _service
                    .MarkAllNotificationsAsReadAsync(
                        employerId);

                return Ok();
            }

            [HttpGet("notification-settings/{employerId:guid}")]
            public async Task<IActionResult> GetSettings(
                Guid employerId)
            {
                var result = await _service
                    .GetNotificationSettingsAsync(
                        employerId);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }

            [HttpPatch("notification-settings/{employerId:guid}")]
            public async Task<IActionResult> UpdateSettings(
                Guid employerId,
                [FromBody]
            UpdateNotificationSettingsDto request)
            {
                var result = await _service
                    .UpdateNotificationSettingsAsync(
                        employerId,
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
    }

