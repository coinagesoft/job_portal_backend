using JobPortal.Application.DTOs.Recruiter.Settings;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/settings")]
    public class RecruiterSettingsController : ControllerBase
    {
        private readonly IRecruiterSettingsService _settingsService;

        public RecruiterSettingsController(
            IRecruiterSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        #region Account

        [HttpGet("account/{employerId:guid}")]
        public async Task<IActionResult> GetAccountSettings(
            Guid employerId)
        {
            var result = await _settingsService
                .GetAccountSettingsAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Account settings not found."
                });
            }

            return Ok(result);
        }

        [HttpPatch("account/{employerId:guid}")]
        public async Task<IActionResult> UpdateAccountSettings(
            Guid employerId,
            [FromForm] UpdateAccountSettingsRequestDto request)
        {
            var result = await _settingsService
                .UpdateAccountSettingsAsync(
                    employerId,
                    request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region Notifications

        [HttpGet("notifications/{employerId:guid}")]
        public async Task<IActionResult> GetNotificationSettings(
            Guid employerId)
        {
            var result = await _settingsService
                .GetNotificationSettingsAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Notification settings not found."
                });
            }

            return Ok(result);
        }

        [HttpPatch("notifications/{employerId:guid}")]
        public async Task<IActionResult> UpdateNotificationSettings(
            Guid employerId,
            [FromForm] UpdateNotificationSettingsRequestDto request)
        {
            var result = await _settingsService.UpdateNotificationSettingsAsync(employerId,request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region Preferences

        [HttpGet("preferences/{employerId:guid}")]
        public async Task<IActionResult> GetPreferenceSettings(Guid employerId)
        {
            var result = await _settingsService.GetPreferenceSettingsAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Preferences not found."
                });
            }

            return Ok(result);
        }

        [HttpPatch("preferences/{employerId:guid}")]
        public async Task<IActionResult> UpdatePreferenceSettings(Guid employerId,
            [FromForm] UpdatePreferenceSettingsRequestDto request)
        {
            var result = await _settingsService
                .UpdatePreferenceSettingsAsync(
                    employerId,
                    request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region Sessions

        [HttpGet("sessions/{employerId:guid}")]
        public async Task<IActionResult> GetUserSessions(
            Guid employerId)
        {
            var result = await _settingsService
                .GetUserSessionsAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Sessions not found."
                });
            }

            return Ok(result);
        }

        [HttpPatch("sessions/revoke/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(
            Guid sessionId)
        {
            var result = await _settingsService
                .RevokeSessionAsync(sessionId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion
    }
}