// File: JobPortal.API/Controllers/Recruiter/RecruiterCreditPlanController.cs

using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    /// <summary>
    /// Recruiter — Credit Plan Purchase
    /// </summary>
    [ApiController]
    [Route("api/recruiter/plans")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterCreditPlanController : ControllerBase
    {
        private readonly IRecruiterCreditPlanService _service;
        private readonly ILogger<RecruiterCreditPlanController> _logger;

        public RecruiterCreditPlanController(
            IRecruiterCreditPlanService service,
            ILogger<RecruiterCreditPlanController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // EmployerId is resolved from the signed JWT rather than a
        // client-supplied header — the token already carries it for both
        // the account owner and any of their sub-users (see
        // RecruiterAuthService.GenerateUserTokenAsync).
        private Guid GetEmployerId()
        {
            var employerId = User.FindFirst("EmployerId")?.Value;

            if (string.IsNullOrWhiteSpace(employerId))
                throw new UnauthorizedAccessException(
                    "Employer ID not found in token.");

            return Guid.Parse(employerId);
        }

        // ── GET /api/recruiter/plans ─────────────────────────────────
        /// <summary>
        /// Get all active credit plans available for purchase.
        /// </summary>
        /// <remarks>
        /// No body required. Returns the plan list for the recruiter's
        /// pricing / plan-selection page.
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetActivePlans()
        {
            var plans = await _service.GetActivePlansAsync();
            return Ok(plans);
        }

        // ── POST /api/recruiter/plans/create-order ───────────────────
        /// <summary>
        /// Step 1 — Create a Razorpay payment order for the chosen plan.
        /// </summary>
        /// <remarks>
        /// Pass only PlanId (body) — EmployerId now comes from the JWT.
        /// UserId is resolved automatically from the employer profile.
        ///
        /// The response contains razorpayOrderId + razorpayKeyId
        /// that the frontend uses to open the Razorpay checkout modal.
        ///
        /// **Swagger headers needed:**
        /// - Authorization: Bearer {jwt}
        ///
        /// **Body:**
        /// ```json
        /// { "planId": "00000000-0000-0000-0000-000000000000" }
        /// ```
        /// </remarks>
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreatePlanOrderRequestDto request)
        {
            if (request.PlanId == Guid.Empty)
                return BadRequest(new { Success = false, Message = "PlanId is required." });

            var result = await _service.CreatePlanOrderAsync(GetEmployerId(), request);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── POST /api/recruiter/plans/verify-payment ─────────────────
        /// <summary>
        /// Step 2 — Verify Razorpay payment, credit the wallet, record purchase.
        /// </summary>
        /// <remarks>
        /// Called by the frontend immediately after the Razorpay modal
        /// closes with a successful payment.
        ///
        /// **Swagger headers needed:**
        /// - Authorization: Bearer {jwt}
        ///
        /// **Body:**
        /// ```json
        /// {
        ///   "transactionId": "guid-from-create-order-response",
        ///   "razorpayOrderId": "order_XXXXXXXXXX",
        ///   "razorpayPaymentId": "pay_XXXXXXXXXX",
        ///   "razorpaySignature": "hmac-sha256-hex-string"
        /// }
        /// ```
        ///
        /// Credits are added **only** after server-side HMAC-SHA256
        /// signature verification passes.
        /// </remarks>
        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment(
            [FromBody] VerifyPlanPaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
                string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                string.IsNullOrWhiteSpace(request.RazorpaySignature))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "RazorpayOrderId, RazorpayPaymentId and RazorpaySignature are all required."
                });
            }

            var result = await _service.VerifyPlanPaymentAsync(GetEmployerId(), request);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}