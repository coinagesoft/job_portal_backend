using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.Settings;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Admin
{
    /// <summary>
    /// Backs the "/admin/settings" screen (Default Language + Session
    /// Timeout). Always operates on the calling admin — the id comes from
    /// the "AdminId" JWT claim, same claim AuditLogMiddleware uses.
    /// </summary>
    [ApiController]
    [Route("api/admin/settings")]
    [Authorize(Roles = "Admin")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly IAdminSettingsService _settingsService;

        public AdminSettingsController(IAdminSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private Guid? GetAdminId()
        {
            var claim = User.FindFirst("AdminId")?.Value;
            return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
        }

        /// <summary>GET api/admin/settings</summary>
        [HttpGet]
        [SkipAuditLog]
        public async Task<IActionResult> GetSettings()
        {
            var adminId = GetAdminId();
            if (adminId == null)
                return Unauthorized(new { success = false, message = "Admin identity not found on token." });

            var result = await _settingsService.GetSettingsAsync(adminId.Value);
            return Ok(result);
        }

        /// <summary>PUT api/admin/settings</summary>
        [HttpPut]
        [AuditLog("Update Admin Settings", "Settings", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateAdminSettingsRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = GetAdminId();
            if (adminId == null)
                return Unauthorized(new { success = false, message = "Admin identity not found on token." });

            var (success, error, data) = await _settingsService.UpdateSettingsAsync(adminId.Value, request);

            if (!success)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, message = "Settings saved.", data });
        }
    }
}