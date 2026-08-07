using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.Auth;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        #region Send OTP

        [HttpPost("send-otp")]
        [AllowAnonymous]
        [SkipAuditLog]
        public async Task<IActionResult> SendOtp(
            [FromBody] AdminSendOtpRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var result = await _authService.SendOtpAsync(
                request,
                ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost("resend-otp")]
        [AllowAnonymous]
        [SkipAuditLog]
        public async Task<IActionResult> ResendOtp(
    [FromBody] AdminResendOtpRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var result = await _authService.ResendOtpAsync(
                request,
                ipAddress);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        #endregion

        #region Verify OTP

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        [SkipAuditLog]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] AdminVerifyOtpRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.VerifyOtpAsync(
                request,
                ipAddress,
                userAgent);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(
    RefreshTokenRequestDto request)
        {
            var result =
                await _authService.RefreshTokenAsync(request);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        #endregion

        #region Logout

        [Authorize]
        [HttpPost("logout")]
        [SkipAuditLog]
        public async Task<IActionResult> Logout()
        {
            var adminId = User.GetAdminId();

            var result = await _authService.LogoutAsync(adminId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Current Admin

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var adminId = User.GetAdminId();

            var result = await _authService.GetCurrentAdminAsync(adminId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        #endregion
    }
}