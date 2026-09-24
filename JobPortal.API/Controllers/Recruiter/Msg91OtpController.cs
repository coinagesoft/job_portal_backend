using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.Controllers
{
    [ApiController]
    [Route("api/msg91-otp")]
    public class Msg91OtpController : ControllerBase
    {
        private readonly ITwilioOtpService _otpService;

        public Msg91OtpController(
            ITwilioOtpService otpService)
        {
            _otpService = otpService;
        }

        //[HttpPost("verify-access-token")]
        //public async Task<IActionResult> VerifyAccessToken(
        //    [FromBody] VerifyAccessTokenRequest request)
        //{
        //    if (request == null ||
        //        string.IsNullOrWhiteSpace(request.AccessToken))
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = "MSG91 access token is required."
        //        });
        //    }

        //    var verified = await _otpService
        //        .VerifyAccessTokenAsync(request.AccessToken);

        //    if (!verified)
        //    {
        //        return Unauthorized(new
        //        {
        //            success = false,
        //            message = "OTP verification failed."
        //        });
        //    }

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "OTP verified successfully."
        //    });
        //}
    }

    public class VerifyAccessTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}