using JobPortal.Application.DTOs.Admin.Auth;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Step 1 — Verify admin exists in DB before triggering Firebase OTP.
        /// Call this BEFORE asking Firebase to send OTP on the frontend.
        /// </summary>
        [HttpPost("check-admin")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(
            typeof(CheckAdminResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(423)]
        public async Task<IActionResult> CheckAdmin(
            [FromBody]
            CheckAdminRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip =
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString() ?? "unknown";

            var response =
                await _authService
                    .CheckAdminExistsAsync(
                        request,
                        ip);

            if (!response.Success)
            {
                return response.Message switch
                {
                    var m when m.Contains("suspended")
                        => StatusCode(403, response),

                    var m when m.Contains("locked")
                        => StatusCode(423, response),

                    _ => Unauthorized(response)
                };
            }

            return Ok(response);
        }

        [HttpPost("generate-firebase-token")]
        public async Task<IActionResult>
    GenerateFirebaseToken(
        [FromBody]
        FirebaseCustomTokenRequestDto request)
        {
            var response =
                await _authService
                    .GenerateFirebaseCustomTokenAsync(
                        request);

            return Ok(response);
        }

        /// <summary>
        /// Step 2 — After Firebase OTP verified on frontend,
        /// exchange Firebase token for JWT.
        /// </summary>
        [HttpPost("firebase-login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> FirebaseLogin(
            [FromBody]
            FirebaseLoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip =
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString() ?? "unknown";

            var response =
                await _authService
                    .FirebaseLoginAsync(
                        request,
                        ip);

            if (!response.Success)
            {
                return response.Message switch
                {
                    var m when m.Contains("suspended")
                        => StatusCode(403, response),

                    var m when m.Contains("locked")
                        => StatusCode(423, response),

                    var m when m.Contains("denied")
                           || m.Contains("Invalid")
                           || m.Contains("expired")
                        => Unauthorized(response),

                    _ => BadRequest(response)
                };
            }

            return Ok(response);
        }
    }
}