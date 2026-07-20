using JobPortal.Application.DTOs.Recruiter.Settings;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/settings")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterSettingsController : ControllerBase
    {
        private readonly IRecruiterSettingsService _settingsService;

        public RecruiterSettingsController(
            IRecruiterSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private bool GetIsSubUser() =>
            User.FindFirst("IsSubUser")?.Value == "true";

        // Every write action in this controller — account details, email/
        // mobile change, notifications, preferences, sessions, and every
        // danger-zone action — is restricted to the account owner. This is
        // the same convention as CompanyProfileController: no sub-user,
        // regardless of their individual permission flags or sub-user
        // role, may change these settings or touch the danger zone. Reads
        // (the GET endpoints) stay open to sub-users so the page can still
        // render for the ones the frontend lets through (HR_Manager role,
        // view-only — see SubUserViewOnlyGuard on the client).
        private IActionResult? BlockIfSubUser()
        {
            if (!GetIsSubUser())
            {
                return null;
            }

            return StatusCode(403, new
            {
                Success = false,
                Message = "You don't have permission to change account settings. Please contact your account owner."
            });
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
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

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

        [HttpPost("account/{employerId:guid}/email/request-otp")]
        public async Task<IActionResult> RequestEmailChangeOtp(
            Guid employerId,
            [FromBody] RequestEmailChangeOtpRequestDto request)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .RequestEmailChangeOtpAsync(employerId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("account/{employerId:guid}/email/verify-otp")]
        public async Task<IActionResult> VerifyEmailChangeOtp(
            Guid employerId,
            [FromBody] VerifyEmailChangeOtpRequestDto request)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .VerifyEmailChangeOtpAsync(employerId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("account/{employerId:guid}/mobile/request-otp")]
        public async Task<IActionResult> RequestMobileChangeOtp(
            Guid employerId,
            [FromBody] RequestMobileChangeOtpRequestDto request)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .RequestMobileChangeOtpAsync(employerId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("account/{employerId:guid}/mobile/verify-otp")]
        public async Task<IActionResult> VerifyMobileChangeOtp(
            Guid employerId,
            [FromBody] VerifyMobileChangeOtpRequestDto request)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .VerifyMobileChangeOtpAsync(employerId, request);

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
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService.UpdateNotificationSettingsAsync(employerId, request);

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
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

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
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .RevokeSessionAsync(sessionId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region Danger Zone

        [HttpPatch("deactivate/{employerId:guid}")]
        public async Task<IActionResult> DeactivateAccount(
            Guid employerId)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .DeactivateAccountAsync(employerId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("jobs/{employerId:guid}")]
        public async Task<IActionResult> DeleteAllJobs(
            Guid employerId)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .DeleteAllJobsAsync(employerId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("account/{employerId:guid}")]
        public async Task<IActionResult> DeleteAccount(
            Guid employerId)
        {
            if (BlockIfSubUser() is IActionResult blocked) return blocked;

            var result = await _settingsService
                .DeleteAccountAsync(employerId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion
    }
}