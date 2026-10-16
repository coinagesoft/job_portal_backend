using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.ICoupon;
using JobPortal.Services.Implement;
using JobPortal.Services.Implement.Candidate;
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
    private readonly VisitorCountryService _visitorCountry;
    private readonly CandidateMembershipPaymentService _payments;
    public CandidateAuthController(
        ICandidateAuthService service,
          ICouponValidationService couponValidationService,
        VisitorCountryService visitorCountry,
        CandidateMembershipPaymentService payments,
        ILogger<CandidateAuthController> logger)
    {
        _service = service;
        _couponValidationService = couponValidationService;
        _visitorCountry = visitorCountry;
        _payments = payments;
        _logger = logger;
    }

    private Task<VisitorCountryResult?> ResolveVisitorAsync(CancellationToken ct) =>
        _visitorCountry.ResolveAsync(HttpContext.Connection.RemoteIpAddress, ct);

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

    // ── Candidate membership payment ─────────────────────────────
    // India         -> Razorpay : POST create-order, then register with the
    //                             Razorpay order/payment/signature.
    // Outside India -> PayPal   : POST paypal/create-order, PayPal popup,
    //                             POST paypal/capture, then register with
    //                             payPalOrderId.
    // The gateway is decided from the visitor's IP on the server.

    /// <summary>Razorpay order (India only).</summary>
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(
    [FromBody] CreateCandidateOrderRequestDto request,
    CancellationToken cancellationToken)
    {
        // Razorpay is for India only. Same rule as the recruiter flows: only
        // refuse when another country is positively detected, so a brief
        // geolocation outage doesn't block Indian candidates.
        var visitor = await ResolveVisitorAsync(cancellationToken);

        if (visitor is not null && visitor.PaymentGateway != "Razorpay")
        {
            return StatusCode(403, new
            {
                Success = false,
                Message = "Razorpay is only available for payments from India. Please pay with PayPal."
            });
        }

        // India pricing region is fixed; never take it from the client.
        request.Region = "in";

        var result =
            await _service.CreateOrderAsync(request);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>PayPal order (outside India only). Region/price/currency come from the server.</summary>
    [HttpPost("paypal/create-order")]
    [ProducesResponseType(typeof(CreateCandidateOrderResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePayPalOrder(
        [FromBody] CreateCandidatePayPalOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Fail closed: PayPal needs a positively detected non-India country.
        var visitor = await ResolveVisitorAsync(cancellationToken);

        if (visitor is null)
        {
            return StatusCode(503, new
            {
                Success = false,
                Message = "We could not detect your country. Please refresh the page or contact support."
            });
        }

        if (visitor.PaymentGateway != "PayPal")
        {
            return StatusCode(403, new
            {
                Success = false,
                Message = "PayPal is not available for payments from India. Please pay with Razorpay."
            });
        }

        var result = await _payments.CreatePayPalOrderAsync(request, visitor.Region);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>Capture the approved PayPal order. Then send payPalOrderId when registering.</summary>
    [HttpPost("paypal/capture")]
    [ProducesResponseType(typeof(CaptureCandidatePayPalResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CapturePayPalOrder(
        [FromBody] CaptureCandidatePayPalRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _payments.CapturePayPalOrderAsync(request);

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