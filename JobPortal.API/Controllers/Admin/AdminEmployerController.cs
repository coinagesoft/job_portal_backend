using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    /// <summary>
    /// Support/admin-side actions on employer accounts. Currently just
    /// covers reactivating an account the employer deactivated from
    /// Settings ▸ Deactivate Account — the employer has no self-service
    /// way back in since a Suspended account is blocked at login.
    ///
    /// NOTE: follows the same [FromHeader(Name = "AdminId")] pattern as
    /// AdminCreditPlanController for now (no [Authorize] on this route,
    /// matching the rest of the current admin module). Tighten this to
    /// require an authenticated Admin role once the admin auth flow is
    /// wired through Swagger/the admin panel.
    /// </summary>
    [ApiController]
    [Route("api/admin/employers")]
    public class AdminEmployerController : ControllerBase
    {
        private readonly IRecruiterSettingsService _settingsService;

        public AdminEmployerController(
            IRecruiterSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpPatch("{employerId:guid}/reactivate")]
        public async Task<IActionResult> ReactivateEmployer(
            Guid employerId,
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result = await _settingsService
                .ReactivateAccountAsync(employerId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}