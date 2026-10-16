using System.Security.Cryptography;
using System.Text;
using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICoupon;
using JobPortal.Services.Implement.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

/// <summary>
/// Candidate membership payments, for every way a candidate can register
/// (mobile/email OTP, Google, LinkedIn):
///
///   India           -> Razorpay (INR)
///   Outside India   -> PayPal   (currency of the visitor's pricing region)
///
/// The gateway is decided by the server from the visitor's IP (see
/// <see cref="VisitorCountryService"/>); nothing in here trusts a client-supplied
/// amount, currency or gateway.
///
/// The candidate pays BEFORE the account exists, so there is no PaymentTransaction
/// to reconcile against up front. Instead:
///   1. <see cref="CreatePayPalOrderAsync"/> creates the PayPal order and binds it
///      (custom_id) to the credential the registration will present.
///   2. <see cref="CapturePayPalOrderAsync"/> captures it after the buyer approves.
///   3. The register endpoint calls <see cref="VerifyPaymentAsync"/>, which re-checks
///      everything against PayPal / Razorpay and our own plan price, and then
///      <see cref="RecordPaymentAsync"/> inside the registration transaction.
/// </summary>
public sealed class CandidateMembershipPaymentService
{
    public const string GatewayRazorpay = "Razorpay";
    public const string GatewayPayPal = "PayPal";

    private const string IndiaRegion = "in";
    private const string OtpPurpose = "CandidateRegistration";

    private readonly AppDbContext _context;
    private readonly ICouponValidationService _couponValidationService;
    private readonly PayPalPaymentService _payPal;
    private readonly IConfiguration _config;
    private readonly ILogger<CandidateMembershipPaymentService> _logger;

    public CandidateMembershipPaymentService(
        AppDbContext context,
        ICouponValidationService couponValidationService,
        PayPalPaymentService payPal,
        IConfiguration config,
        ILogger<CandidateMembershipPaymentService> logger)
    {
        _context = context;
        _couponValidationService = couponValidationService;
        _payPal = payPal;
        _config = config;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════
    // PayPal — create order (visitors outside India)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// custom_id stamped on the PayPal order. Derived from the credential the
    /// registration will present (OTP token / provider access token), hashed so
    /// the credential itself is never sent to PayPal.
    /// </summary>
    public static string PayPalRef(string bindingSecret)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(bindingSecret));
        return "candmem-" + Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    public async Task<CreateCandidateOrderResponseDto> CreatePayPalOrderAsync(
        CreateCandidatePayPalOrderRequestDto request,
        string visitorRegion)
    {
        CreateCandidateOrderResponseDto Fail(string message) =>
            new() { Success = false, PaymentGateway = GatewayPayPal, Message = message };

        try
        {
            var bindingSecret = ResolveBindingSecret(request.OtpToken, request.AccessToken);

            if (bindingSecret == null)
                return Fail("Please verify your mobile number or email before paying.");

            // OTP registration: the token must be a real, verified one.
            if (!string.IsNullOrWhiteSpace(request.OtpToken))
            {
                var otpOk = await _context.OtpVerifications.AnyAsync(x =>
                    x.VerificationToken == request.OtpToken &&
                    x.IsVerified &&
                    x.Purpose == OtpPurpose);

                if (!otpOk)
                    return Fail("OTP verification required.");
            }

            // Region comes from the server-side IP lookup, not the client.
            var region = (visitorRegion ?? string.Empty).Trim().ToLowerInvariant();

            if (region.Length == 0 || region == IndiaRegion)
                return Fail("PayPal is not available for payments from India.");

            var plan = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p =>
                    p.PlanType == PlanType.Candidate &&
                    p.IsActive &&
                    p.Region.ToLower() == region)
                .OrderBy(p => p.Price)
                .FirstOrDefaultAsync();

            if (plan == null)
                return Fail("Candidate membership is not available for your region right now.");

            var currency = VisitorCountryService.CurrencyForRegion(plan.Region);

            if (!VisitorCountryService.IsPayPalCurrency(currency))
            {
                _logger.LogError(
                    "Candidate plan {PlanId} (region {Region}) is priced in {Currency}, which PayPal can't settle.",
                    plan.PlanId, plan.Region, currency);

                return Fail("This plan can't be paid with PayPal. Please contact support.");
            }

            if (plan.Price <= 0)
                return Fail("This membership plan doesn't require payment.");

            var originalAmount = plan.Price;
            decimal discountAmount = 0;
            string? appliedCouponCode = null;

            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var couponResult = await _couponValidationService.ValidateCouponAsync(
                    new ValidateCouponRequestDto
                    {
                        Code = request.CouponCode.Trim().ToUpperInvariant(),
                        PlanType = PlanType.Candidate,
                        PlanId = plan.PlanId,
                        Region = plan.Region
                    },
                    Guid.Empty);

                if (!couponResult.IsValid)
                    return Fail(couponResult.Message);

                discountAmount = Math.Clamp(couponResult.DiscountAmount, 0, originalAmount);
                appliedCouponCode = couponResult.CouponCode;
            }

            var finalAmount = originalAmount - discountAmount;

            if (ToMinor(finalAmount) <= 0)
                return Fail("The final payment amount must be greater than zero.");

            var order = await _payPal.CreateOrderAsync(
                finalAmount,
                currency,
                PayPalRef(bindingSecret));

            return new CreateCandidateOrderResponseDto
            {
                Success = true,
                PaymentGateway = GatewayPayPal,
                PayPalOrderId = order.OrderId,
                PayPalClientId = _config["PayPal:ClientId"],
                Currency = currency,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Amount = originalAmount,
                AmountPaise = ToMinor(originalAmount),
                DiscountAmount = discountAmount,
                DiscountAmountPaise = ToMinor(discountAmount),
                FinalAmount = finalAmount,
                FinalAmountPaise = ToMinor(finalAmount),
                CouponCode = appliedCouponCode,
                Message = "Order created successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate CreatePayPalOrderAsync error");
            return Fail("Payment gateway error. Please try again.");
        }
    }

    // ════════════════════════════════════════════════════════════
    // PayPal — capture the approved order
    // ════════════════════════════════════════════════════════════

    public async Task<CaptureCandidatePayPalResponseDto> CapturePayPalOrderAsync(
        CaptureCandidatePayPalRequestDto request)
    {
        CaptureCandidatePayPalResponseDto Fail(string message) =>
            new() { Success = false, Message = message, PayPalOrderId = request.PayPalOrderId };

        try
        {
            if (string.IsNullOrWhiteSpace(request.PayPalOrderId))
                return Fail("PayPalOrderId is required.");

            var bindingSecret = ResolveBindingSecret(request.OtpToken, request.AccessToken);

            if (bindingSecret == null)
                return Fail("Registration verification is missing. Please start again.");

            // The order must have been created for THIS registration.
            var existing = await _payPal.GetOrderAsync(request.PayPalOrderId);

            if (!string.Equals(existing.CustomId, PayPalRef(bindingSecret), StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Candidate PayPal capture refused: order {OrderId} was not created for this registration.",
                    request.PayPalOrderId);

                return Fail("This PayPal order does not belong to your registration.");
            }

            var capture = existing.IsCompleted
                ? existing
                : await _payPal.CaptureOrderAsync(request.PayPalOrderId);

            if (!capture.IsCompleted)
            {
                _logger.LogWarning(
                    "Candidate PayPal capture not completed. Order={OrderId} Status={Status}/{CaptureStatus}",
                    request.PayPalOrderId, capture.Status, capture.CaptureStatus);

                return Fail("PayPal has not completed this payment. If money was deducted, contact support with your PayPal order reference.");
            }

            // Amount / currency / plan are verified again when the registration is submitted.
            return new CaptureCandidatePayPalResponseDto
            {
                Success = true,
                Captured = true,
                PayPalOrderId = request.PayPalOrderId,
                Message = "Payment captured."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate CapturePayPalOrderAsync error. Order={OrderId}", request.PayPalOrderId);
            return Fail("We couldn't confirm your PayPal payment. If money was deducted, contact support with your PayPal order reference.");
        }
    }

    // ════════════════════════════════════════════════════════════
    // Verify a payment at registration time (Razorpay or PayPal)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Proves the candidate really paid the right amount for the plan.
    /// Pass the new user's id so a coupon's per-user limit can be enforced.
    /// Does not write anything.
    /// </summary>
    public async Task<CandidatePaymentVerification> VerifyPaymentAsync(
        CandidatePaymentProof proof,
        Guid userId)
    {
        try
        {
            var usePayPal = !string.IsNullOrWhiteSpace(proof.PayPalOrderId);

            if (!usePayPal &&
                (string.IsNullOrWhiteSpace(proof.RazorpayOrderId) ||
                 string.IsNullOrWhiteSpace(proof.RazorpayPaymentId) ||
                 string.IsNullOrWhiteSpace(proof.RazorpaySignature)))
            {
                return CandidatePaymentVerification.Fail("Payment verification failed.");
            }

            // Never trust a price from the client: re-read the plan from the DB.
            var plan = await _context.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PlanId == proof.PlanId &&
                    p.PlanType == PlanType.Candidate &&
                    p.IsActive);

            if (plan == null)
            {
                return CandidatePaymentVerification.Fail(
                    "Selected membership plan is no longer available. Please refresh and try again.");
            }

            var originalAmount = plan.Price;

            if (originalAmount <= 0)
                return CandidatePaymentVerification.Fail("This membership plan doesn't require payment.");

            // ── Coupon ───────────────────────────────────────────
            decimal discountAmount = 0;
            decimal finalAmount = originalAmount;
            Guid? couponId = null;
            string? appliedCouponCode = null;

            if (!string.IsNullOrWhiteSpace(proof.CouponCode))
            {
                var couponResult = await _couponValidationService.ValidateCouponAsync(
                    new ValidateCouponRequestDto
                    {
                        Code = proof.CouponCode.Trim(),
                        PlanType = PlanType.Candidate,
                        PlanId = plan.PlanId,
                        // Use the region stored on the actual plan.
                        Region = plan.Region
                    },
                    userId);

                if (!couponResult.IsValid)
                    return CandidatePaymentVerification.Fail(couponResult.Message);

                // Always use the amounts calculated by the coupon service.
                discountAmount = couponResult.DiscountAmount;
                finalAmount = couponResult.FinalAmount;
                couponId = couponResult.CouponId;
                appliedCouponCode = couponResult.CouponCode;
            }

            if (discountAmount < 0 || discountAmount > originalAmount)
                return CandidatePaymentVerification.Fail("Invalid coupon discount.");

            if (finalAmount < 0)
                return CandidatePaymentVerification.Fail("Invalid final payment amount.");

            var originalMinor = ToMinor(originalAmount);
            var discountMinor = ToMinor(discountAmount);
            var finalMinor = ToMinor(finalAmount);

            if (finalMinor <= 0)
                return CandidatePaymentVerification.Fail("The final payment amount must be greater than zero.");

            // ── Gateway-specific proof ───────────────────────────
            var verification = new CandidatePaymentVerification
            {
                Success = true,
                Plan = plan,
                OriginalAmountPaise = originalMinor,
                DiscountAmountPaise = discountMinor,
                FinalAmountPaise = finalMinor,
                CouponId = couponId,
                CouponCode = appliedCouponCode
            };

            return usePayPal
                ? await VerifyPayPalAsync(proof, plan, finalMinor, verification)
                : await VerifyRazorpayAsync(proof, finalMinor, verification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate VerifyPaymentAsync error");
            return CandidatePaymentVerification.Fail(
                "Payment verification failed. Please contact support if the amount was deducted.");
        }
    }

    private async Task<CandidatePaymentVerification> VerifyRazorpayAsync(
        CandidatePaymentProof proof,
        int finalMinor,
        CandidatePaymentVerification verification)
    {
        // Signature proves the payment belongs to that Razorpay order.
        if (!VerifyRazorpaySignature(
                proof.RazorpayOrderId!,
                proof.RazorpayPaymentId!,
                proof.RazorpaySignature!))
        {
            return CandidatePaymentVerification.Fail("Payment verification failed.");
        }

        // A given Razorpay payment can only ever fund one registration.
        var alreadyUsed = await _context.PaymentTransactions.AnyAsync(t =>
            t.RazorpayPaymentId == proof.RazorpayPaymentId &&
            t.PaymentStatus == "Completed");

        if (alreadyUsed)
        {
            return CandidatePaymentVerification.Fail(
                "This payment has already been used to complete a registration.");
        }

        // ...and the order must be for exactly the amount WE calculated.
        var client = new Razorpay.Api.RazorpayClient(
            _config["Razorpay:KeyId"],
            _config["Razorpay:KeySecret"]);

        var razorpayOrder = client.Order.Fetch(proof.RazorpayOrderId);

        // The Razorpay SDK's indexer returns dynamic; give these real types so the
        // logger extension method below can be called (CS1973 otherwise).
        int orderAmountPaise = Convert.ToInt32(razorpayOrder["amount"]);
        string? orderCurrency = Convert.ToString(razorpayOrder["currency"]);

        if (orderAmountPaise != finalMinor ||
            !string.Equals(orderCurrency, "INR", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Razorpay candidate order mismatch. Order={OrderId} Expected={Expected} INR Got={Got} {GotCurrency}",
                proof.RazorpayOrderId, finalMinor, orderAmountPaise, orderCurrency);

            return CandidatePaymentVerification.Fail(
                "Payment amount does not match the selected membership plan.");
        }

        verification.Gateway = GatewayRazorpay;
        verification.Currency = "INR";
        verification.RazorpayOrderId = proof.RazorpayOrderId;
        verification.RazorpayPaymentId = proof.RazorpayPaymentId;

        return verification;
    }

    private async Task<CandidatePaymentVerification> VerifyPayPalAsync(
        CandidatePaymentProof proof,
        MembershipPlan plan,
        int finalMinor,
        CandidatePaymentVerification verification)
    {
        if (string.IsNullOrWhiteSpace(proof.BindingSecret))
            return CandidatePaymentVerification.Fail("Registration verification is missing. Please start again.");

        var expectedCurrency = VisitorCountryService.CurrencyForRegion(plan.Region ?? string.Empty);

        // India plans are Razorpay-only, and PayPal must be able to settle the plan's currency.
        if (string.IsNullOrWhiteSpace(plan.Region) ||
            string.Equals(plan.Region.Trim(), IndiaRegion, StringComparison.OrdinalIgnoreCase) ||
            !VisitorCountryService.IsPayPalCurrency(expectedCurrency))
        {
            return CandidatePaymentVerification.Fail("PayPal can't be used for the selected membership plan.");
        }

        PayPalCaptureResult capture;

        try
        {
            capture = await _payPal.GetOrderAsync(proof.PayPalOrderId!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Candidate PayPal order lookup failed at register. Order={OrderId}", proof.PayPalOrderId);
            return CandidatePaymentVerification.Fail("We couldn't confirm your PayPal payment. Please try again.");
        }

        if (!capture.IsCompleted)
            return CandidatePaymentVerification.Fail("PayPal payment is not completed.");

        if (!string.Equals(capture.CustomId, PayPalRef(proof.BindingSecret), StringComparison.Ordinal))
        {
            return CandidatePaymentVerification.Fail(
                "This PayPal payment does not belong to your registration.");
        }

        if (!string.Equals(capture.CapturedCurrency, expectedCurrency, StringComparison.OrdinalIgnoreCase) ||
            capture.CapturedAmount is null ||
            ToMinor(capture.CapturedAmount.Value) != finalMinor)
        {
            _logger.LogError(
                "PayPal candidate membership amount mismatch. Order={OrderId} Capture={CaptureId} Expected={Expected} {Currency} Got={Got} {GotCurrency}. Needs manual refund.",
                capture.OrderId, capture.CaptureId, finalMinor / 100m, expectedCurrency,
                capture.CapturedAmount, capture.CapturedCurrency);

            return CandidatePaymentVerification.Fail(
                "Payment amount does not match the selected membership plan. Please contact support.");
        }

        if (string.IsNullOrWhiteSpace(capture.CaptureId))
            return CandidatePaymentVerification.Fail("PayPal payment could not be verified.");

        var alreadyUsed = await _context.PaymentTransactions.AnyAsync(t =>
            t.PayPalCaptureId == capture.CaptureId &&
            t.PaymentStatus == "Completed");

        if (alreadyUsed)
        {
            return CandidatePaymentVerification.Fail(
                "This payment has already been used to complete a registration.");
        }

        verification.Gateway = GatewayPayPal;
        verification.Currency = expectedCurrency;
        verification.PayPalOrderId = proof.PayPalOrderId;
        verification.PayPalCaptureId = capture.CaptureId;

        return verification;
    }

    // ════════════════════════════════════════════════════════════
    // Record a verified payment (inside the registration transaction)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds the PaymentTransaction, CouponRedemption and Invoice for a payment that
    /// <see cref="VerifyPaymentAsync"/> accepted. MUST be called while the caller's
    /// database transaction is open (the invoice-number advisory lock and the atomic
    /// coupon counter both rely on it). Does not save or commit.
    /// Returns an error message when the coupon ran out in the meantime (the caller
    /// must roll back), otherwise null.
    /// </summary>
    public async Task<string?> RecordPaymentAsync(
        User user,
        CandidateProfile profile,
        CandidatePaymentVerification payment)
    {
        // ── Atomically consume the coupon ─────────────────────
        // Increment UsedCount only while the coupon is active and under its
        // usage limit, so two simultaneous registrations can't exceed it.
        if (payment.CouponId.HasValue)
        {
            var couponId = payment.CouponId.Value;

            var updated = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"
                UPDATE coupons
                SET used_count = used_count + 1
                WHERE coupon_id = {couponId}
                  AND is_active = TRUE
                  AND (
                        usage_limit IS NULL
                        OR used_count < usage_limit
                      )
                ");

            if (updated != 1)
                return "This coupon is no longer available. Please try again.";
        }

        var isPayPal = payment.Gateway == GatewayPayPal;

        // The PaymentTransaction *Paise columns are legacy-named: for PayPal they hold
        // minor units (cents/pence) of the plan's currency.
        var paymentTransaction = new PaymentTransaction
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.UserId,
            CandidateId = profile.CandidateId,
            TransactionType = "CandidateRegistration",
            PackType = payment.Plan.PlanName,
            CreditQuantity = null,
            ValidityMonths = null,

            // Original membership plan price
            AmountPaise = payment.OriginalAmountPaise,
            // Coupon discount
            DiscountAmountPaise = payment.DiscountAmountPaise,
            GstAmountPaise = 0,
            // Actual amount charged by the gateway
            TotalAmountPaise = payment.FinalAmountPaise,

            PaymentMethod = payment.Gateway,
            RazorpayOrderId = isPayPal ? null : payment.RazorpayOrderId,
            RazorpayPaymentId = isPayPal ? null : payment.RazorpayPaymentId,
            PayPalOrderId = isPayPal ? payment.PayPalOrderId : null,
            PayPalCaptureId = isPayPal ? payment.PayPalCaptureId : null,

            PaymentStatus = "Completed",
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(paymentTransaction);

        if (payment.CouponId.HasValue && !string.IsNullOrWhiteSpace(payment.CouponCode))
        {
            _context.CouponRedemptions.Add(new CouponRedemption
            {
                RedemptionId = Guid.NewGuid(),
                CouponId = payment.CouponId.Value,
                UserId = user.UserId,
                PlanId = payment.Plan.PlanId,
                PaymentTransactionId = paymentTransaction.TransactionId,
                CouponCode = payment.CouponCode,
                OriginalAmountPaise = payment.OriginalAmountPaise,
                DiscountAmountPaise = payment.DiscountAmountPaise,
                FinalAmountPaise = payment.FinalAmountPaise,
                RedeemedAt = DateTime.UtcNow,
                PaymentTransaction = paymentTransaction
            });
        }

        // Invoice amount columns are int (whole currency units). Razorpay keeps its
        // existing truncating behaviour; PayPal rounds so 49.99 USD isn't stored as 49.
        var invoiceWhole = isPayPal
            ? (int)Math.Round(payment.FinalAmountPaise / 100m, MidpointRounding.AwayFromZero)
            : payment.FinalAmountPaise / 100;

        _context.Invoices.Add(new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            TransactionId = paymentTransaction.TransactionId,
            UserId = user.UserId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            InvoiceAmount = invoiceWhole,
            InvoiceGst = 0,
            InvoiceTotal = invoiceWhole,
            InvoiceS3Url = null,
            CreatedAt = DateTime.UtcNow,
            PaymentTransaction = paymentTransaction
        });

        return null;
    }

    // ════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════

    private static string? ResolveBindingSecret(string? otpToken, string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(otpToken))
            return otpToken.Trim();

        if (!string.IsNullOrWhiteSpace(accessToken))
            return accessToken.Trim();

        return null;
    }

    private static int ToMinor(decimal value) =>
        (int)Math.Round(value * 100, MidpointRounding.AwayFromZero);

    private bool VerifyRazorpaySignature(string orderId, string paymentId, string signature)
    {
        try
        {
            var attributes = new Dictionary<string, string>
            {
                { "razorpay_order_id", orderId },
                { "razorpay_payment_id", paymentId },
                { "razorpay_signature", signature }
            };

            Razorpay.Api.Utils.verifyPaymentSignature(attributes);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Razorpay signature verification failed. OrderId:{OrderId}",
                orderId);

            return false;
        }
    }

    // Sequential, per-month invoice number, e.g. INV-202607-0001. Same approach as
    // RecruiterCreditPlanService.GenerateInvoiceNumberAsync: a PostgreSQL advisory
    // transaction lock + MAX (not COUNT) so it stays unique. MUST run inside an open
    // db transaction so the lock guards the invoice insert that follows.
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({prefix}))");

        var suffixesThisMonth = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .Select(i => i.InvoiceNumber.Substring(prefix.Length))
            .ToListAsync();

        int maxNumber = suffixesThisMonth
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxNumber + 1):D4}";
    }
}

/// <summary>What the candidate sent to prove they paid.</summary>
public sealed class CandidatePaymentProof
{
    public Guid PlanId { get; init; }

    public string? CouponCode { get; init; }

    // Razorpay (India)
    public string? RazorpayOrderId { get; init; }
    public string? RazorpayPaymentId { get; init; }
    public string? RazorpaySignature { get; init; }

    // PayPal (outside India)
    public string? PayPalOrderId { get; init; }

    /// <summary>
    /// The credential this registration presents (OTP token, or the Google/LinkedIn
    /// access token). A PayPal order is only accepted if it was created for it.
    /// </summary>
    public string BindingSecret { get; init; } = string.Empty;
}

/// <summary>Result of <see cref="CandidateMembershipPaymentService.VerifyPaymentAsync"/>.</summary>
public sealed class CandidatePaymentVerification
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public MembershipPlan Plan { get; init; } = default!;

    // Minor units (paise / cents / pence) of Currency.
    public int OriginalAmountPaise { get; init; }
    public int DiscountAmountPaise { get; init; }
    public int FinalAmountPaise { get; init; }

    public Guid? CouponId { get; init; }
    public string? CouponCode { get; init; }

    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? PayPalOrderId { get; set; }
    public string? PayPalCaptureId { get; set; }

    public static CandidatePaymentVerification Fail(string message) =>
        new() { Success = false, Message = message };
}