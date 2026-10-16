using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.ICoupon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/auth")]
[Produces("application/json")]
public class CandidateAuthController : ControllerBase
{
    private readonly ICandidateAuthService _service;
    private readonly ILogger<CandidateAuthController> _logger;
    private readonly ICouponValidationService _couponValidationService;
    public CandidateAuthController(
        ICandidateAuthService service,
          ICouponValidationService couponValidationService,
        ILogger<CandidateAuthController> logger)
    {
        _service = service;
        _couponValidationService = couponValidationService;
        _logger = logger;
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [HttpPost("register")]
    [ProducesResponseType(typeof(CandidateRegisterResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register(
        [FromBody] CandidateRegisterRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.RegisterAsync(
                request,
                GetIp());

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Candidate Register error");

            return StatusCode(
                500,
                new
                {
                    Success = false,
                    Message = "Internal server error"
                });
        }
    }
    [HttpPost("send-otp")]


    public async Task<IActionResult> SendRegistrationOtp(
     [FromBody] CandidateSendOtpRequestDto request)
    {
        var result =
            await _service.SendRegistrationOtpAsync(
                request,
                GetIp());

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] CandidateVerifyOtpRequestDto request)
    {
        var result =
            await _service.VerifyOtpAsync(
                request,
                GetIp());

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(
    [FromBody] CreateCandidateOrderRequestDto request)
    {
        var result =
            await _service.CreateOrderAsync(request);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("validate-coupon")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCoupon(
    [FromBody] ValidateCouponRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _couponValidationService.ValidateCouponAsync(
            request,
            Guid.Empty);

        return result.IsValid
            ? Ok(result)
            : BadRequest(result);
    }
}