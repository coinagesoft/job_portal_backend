using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.Users;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/sub-admins")]
    [Authorize]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        #region List Sub Admins

        // GET /api/admin/sub-admins
        // Backs the toolbar, table and stat cards on /admin/users.
        [HttpGet]
        [SkipAuditLog]
        public async Task<IActionResult> GetSubAdmins()
        {
            var result = await _adminUserService.GetSubAdminsAsync(new SubAdminListRequestDto());

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Add Sub Admin

        // POST /api/admin/sub-admins
        // Backs the "Add Sub Admin" drawer on /admin/users.
        [HttpPost]
        [SkipAuditLog]
        public async Task<IActionResult> CreateSubAdmin(
            [FromBody] CreateSubAdminRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _adminUserService.CreateSubAdminAsync(
                request,
                createdByAdminId,
                ipAddress,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Edit Sub Admin

        // PUT /api/admin/sub-admins/{id}
        // Backs the "Edit Sub Admin" drawer on /admin/users.
        [HttpPut("{id:guid}")]
        [SkipAuditLog]
        public async Task<IActionResult> UpdateSubAdmin(
            Guid id,
            [FromBody] UpdateSubAdminRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _adminUserService.UpdateSubAdminAsync(
                id,
                request,
                updatedByAdminId,
                ipAddress,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Delete Sub Admin

        // DELETE /api/admin/sub-admins/{id}
        // Backs the "Remove" action on /admin/users. Soft-deletes the
        // sub-admin (see AdminUserService.DeleteSubAdminAsync) rather
        // than hard-deleting the row.
        [HttpDelete("{id:guid}")]
        [SkipAuditLog]
        public async Task<IActionResult> DeleteSubAdmin(Guid id)
        {
            var deletedByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _adminUserService.DeleteSubAdminAsync(
                id,
                deletedByAdminId,
                ipAddress,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Suspend Sub Admin

        // PATCH /api/admin/sub-admins/{id}/suspend
        // Backs the "Suspend" row action on /admin/users. Unlike DELETE,
        // this keeps the sub-admin record — it just blocks login until
        // reactivated.
        [HttpPatch("{id:guid}/suspend")]
        [SkipAuditLog]
        public async Task<IActionResult> SuspendSubAdmin(
            Guid id,
            [FromBody] SuspendSubAdminRequestDto? request)
        {
            var suspendedByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _adminUserService.SuspendSubAdminAsync(
                id,
                request ?? new SuspendSubAdminRequestDto(),
                suspendedByAdminId,
                ipAddress,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Activate Sub Admin

        // PATCH /api/admin/sub-admins/{id}/activate
        // Backs the "Activate" row action on /admin/users, reversing a
        // suspension so the sub-admin can log in again.
        [HttpPatch("{id:guid}/activate")]
        [SkipAuditLog]
        public async Task<IActionResult> ActivateSubAdmin(Guid id)
        {
            var activatedByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _adminUserService.ActivateSubAdminAsync(
                id,
                activatedByAdminId,
                ipAddress,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}