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

        #region Add Sub Admin

        // POST /api/admin/sub-admins
        // Backs the "Add Sub Admin" drawer on /admin/users.
        [HttpPost]
        public async Task<IActionResult> CreateSubAdmin(
            [FromBody] CreateSubAdminRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdByAdminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var result = await _adminUserService.CreateSubAdminAsync(
                request,
                createdByAdminId,
                ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}