using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.ICoupon;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement.Coupons;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razorpay.Api;
using System.Linq;


namespace JobPortal.Services.Implement.Candidate;

public class CandidateAuthService : ICandidateAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CandidateAuthService> _logger;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly IMembershipPlanService _membershipPlanService;
    private const int OtpExpiryMinutes = 10;

    private readonly ICouponValidationService _couponValidationService;
    private readonly IConfiguration _config;
    private readonly CandidateMembershipPaymentService _payments;

    private const int ResendCooldownSeconds = 30;

    private const int MaxOtpAttempts = 5;

    // Default pricing region for the candidate membership fee when the
    // caller doesn't specify one — this flow is currently India-focused.
    private const string DefaultCandidateMembershipRegion = "in";

    public CandidateAuthService(
        AppDbContext context,
        JwtService jwtService,
        IEmailService emailService,
        ITwilioOtpService twilioOtpService,
         IConfiguration config,
           ICouponValidationService couponValidationService,
        IMembershipPlanService membershipPlanService,
        CandidateMembershipPaymentService payments,
        ILogger<CandidateAuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
        _config = config;
        _couponValidationService = couponValidationService;
        _twilioOtpService = twilioOtpService;
        _membershipPlanService = membershipPlanService;
        _payments = payments;
    }

    // Resolves the active candidate membership plan to charge: prefers
    // an exact region match, falls back to the "in" (India) default
    // region, and finally to any active Candidate plan if neither
    // region has one configured yet. Returns null if the admin hasn't
    // configured any active candidate membership plan at all.
    private async Task<JobPortal.Application.DTOs.Admin.MembershipPlan.MembershipPlanResponseDto?> ResolveCandidateMembershipPlanAsync(string? region)
    {
        var normalizedRegion = string.IsNullOrWhiteSpace(region)
            ? DefaultCandidateMembershipRegion
            : region.Trim().ToLowerInvariant();

        var regionPlans = await _membershipPlanService.GetActivePlansAsync(PlanType.Candidate, normalizedRegion);
        var plan = regionPlans.OrderBy(p => p.Price).FirstOrDefault();
        if (plan != null)
            return plan;

        if (normalizedRegion != DefaultCandidateMembershipRegion)
        {
            var defaultRegionPlans = await _membershipPlanService.GetActivePlansAsync(PlanType.Candidate, DefaultCandidateMembershipRegion);
            plan = defaultRegionPlans.OrderBy(p => p.Price).FirstOrDefault();
            if (plan != null)
                return plan;
        }

        var anyPlans = await _membershipPlanService.GetActivePlansAsync(PlanType.Candidate, null);
        return anyPlans.OrderBy(p => p.Price).FirstOrDefault();
    }

    // =====================================================
    // REGISTER
    // =====================================================

    public async Task<CandidateRegisterResponseDto> RegisterAsync(
      CandidateRegisterRequestDto request,
      string ipAddress)
    {
        try
        {
            // ============================================================
            // 1. TERMS & CONDITIONS
            // ============================================================

            if (!request.TermsAccepted)
            {
                return Fail(
                    "Terms and Conditions must be accepted.");
            }

            // ============================================================
            // 2. VERIFY OTP TOKEN
            // ============================================================

            var verifiedOtp =
                await _context.OtpVerifications
                    .FirstOrDefaultAsync(x =>
                        x.VerificationToken == request.OtpToken &&
                        x.IsVerified &&
                        x.Purpose == "CandidateRegistration");

            if (verifiedOtp == null)
            {
                return Fail(
                    "OTP verification required.");
            }

            // ============================================================
            // 3. PAYMENT DETAILS REQUIRED
            // ============================================================

            // India pays with Razorpay (order id + payment id + signature),
            // everywhere else with PayPal (captured order id). Which one is
            // valid, the amount, the currency and replay protection are all
            // verified server-side in step 6.
            var hasPayPalPayment =
                !string.IsNullOrWhiteSpace(request.PayPalOrderId);

            var hasRazorpayPayment =
                !string.IsNullOrWhiteSpace(request.RazorpayPaymentId) &&
                !string.IsNullOrWhiteSpace(request.RazorpayOrderId) &&
                !string.IsNullOrWhiteSpace(request.RazorpaySignature);

            if (!hasPayPalPayment && !hasRazorpayPayment)
            {
                return Fail("Payment verification failed.");
            }

            // ============================================================
            // 4. MOBILE ALREADY REGISTERED?
            // ============================================================

            // A soft-deleted leftover with this mobile/email must not block re-registration.
            await AccountCleanupService.PurgeSoftDeletedCandidatesAsync(
                _context, request.Email, request.MobileNumber);

            var existingMobile =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.MobileNumber == request.MobileNumber &&
                        x.CountryCode == request.CountryCode);

            if (existingMobile != null)
            {
                return Fail(
                    "Mobile number already registered.");
            }

            // ============================================================
            // 5. EMAIL ALREADY REGISTERED?
            // ============================================================

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingEmail =
                    await _context.Users
                        .FirstOrDefaultAsync(x =>
                            x.Email != null &&
                            x.Email.ToLower() ==
                            request.Email.ToLower());

                if (existingEmail != null)
                {
                    return Fail(
                        "Email already registered.");
                }
            }

            // ============================================================
            // 6. CREATE USER ID
            // ============================================================

            // We generate the UserId now so that the coupon validation
            // can correctly enforce the PerUserLimit.
            var user = new User
            {
                UserId = Guid.NewGuid(),

                UserType = UserType.Candidate,

                MobileNumber = request.MobileNumber,

                CountryCode = request.CountryCode,

                // uq_users_email is a plain unique index with no filter
                // for blank values, so an empty string would collide with
                // another candidate who skipped email.
                Email = string.IsNullOrWhiteSpace(request.Email)
                    ? null
                    : request.Email,

                PasswordHash = "OTP_AUTH",

                AccountStatus = AccountStatus.Active,

                KycStatus = KycStatus.Pending,

                PaymentStatus = PaymentStatus.Paid,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            // ============================================================
            // 7. VERIFY THE PAYMENT (Razorpay or PayPal)
            // ============================================================

            // IMPORTANT:
            // Never trust the price from the frontend. The plan, coupon,
            // amount and currency are re-derived from the database and
            // checked against what the gateway says was actually paid.
            // The UserId exists at this point, so a coupon's PerUserLimit
            // can be checked correctly.
            var payment = await _payments.VerifyPaymentAsync(
                new CandidatePaymentProof
                {
                    PlanId = request.PlanId,
                    CouponCode = request.CouponCode,
                    RazorpayOrderId = request.RazorpayOrderId,
                    RazorpayPaymentId = request.RazorpayPaymentId,
                    RazorpaySignature = request.RazorpaySignature,
                    PayPalOrderId = request.PayPalOrderId,
                    // A PayPal order is bound to the OTP token it was created for.
                    BindingSecret = request.OtpToken
                },
                user.UserId);

            if (!payment.Success)
            {
                return Fail(payment.Message);
            }

            // ============================================================
            // 8. ADD USER TO EF CONTEXT
            // ============================================================

            _context.Users.Add(user);

            // ============================================================
            // 9. CREATE CANDIDATE PROFILE
            // ============================================================

            var profile = new CandidateProfile
            {
                CandidateId = Guid.NewGuid(),

                UserId = user.UserId,

                FullName = request.FullName,

                ProfileStatus = "Incomplete",

                ProfileCompletionPct = 0,

                AvailabilityStatus = "Available",

                IsMember = true,

                MembershipPlanId =
                    payment.Plan.PlanId,

                MembershipPurchasedAt =
                    DateTime.UtcNow,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };

            _context.CandidateProfiles.Add(profile);

            // ============================================================
            // 10. BEGIN DATABASE TRANSACTION
            // ============================================================

            // Everything from here must succeed together:
            //
            // User
            // CandidateProfile
            // PaymentTransaction
            // CouponRedemption
            // Coupon UsedCount
            // Invoice
            // OTP consumption
            //
            // Otherwise everything is rolled back.
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            // ============================================================
            // 11. RECORD PAYMENT, COUPON REDEMPTION AND INVOICE
            // ============================================================

            // Also atomically consumes the coupon's usage and generates the
            // invoice number (PostgreSQL advisory lock, hence the open
            // transaction above).
            var recordError = await _payments.RecordPaymentAsync(
                user,
                profile,
                payment);

            if (recordError != null)
            {
                await dbTransaction.RollbackAsync();

                return Fail(recordError);
            }

            // ============================================================
            // 12. CONSUME OTP TOKEN
            // ============================================================

            // Prevent the same OTP verification token from being reused.
            verifiedOtp.VerificationToken = null;

            // ============================================================
            // 13. SAVE EVERYTHING
            // ============================================================

            await _context.SaveChangesAsync();

            // ============================================================
            // 14. COMMIT DATABASE TRANSACTION
            // ============================================================

            await dbTransaction.CommitAsync();

            // ============================================================
            // 15. GENERATE JWT
            // ============================================================

            var (token, _) =
                await _jwtService.GenerateTokenAsync(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber,
                    candidateId:
                        profile.CandidateId);

            // ============================================================
            // 16. SUCCESS RESPONSE
            // ============================================================

            return new CandidateRegisterResponseDto
            {
                Success = true,

                Token = token,

                CandidateId =
                    profile.CandidateId,

                UserName =
                    profile.FullName,

                RedirectTo =
                    "/candidate/profile/setup",

                Message =
                    "Registration successful. Please complete your profile."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Candidate Registration Error. IP:{IP}",
                ipAddress);

            return Fail(
                "An error occurred while registering.");
        }
    }


    public async Task<SendOtpResponseDto> SendRegistrationOtpAsync(
        CandidateSendOtpRequestDto request,
        string ipAddress)
    {
        try
        {
            var identifier =
                request.Identifier.Trim().ToLower();

            var isEmail = IsEmail(identifier);

            if (!isEmail)
            {
                identifier = identifier
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");
            }

            var isMobile = IsMobile(identifier);

            if (!isEmail && !isMobile)
            {
                return SendFail(
                    "Please enter a valid email or mobile number.");
            }

            if (isMobile &&
                string.IsNullOrWhiteSpace(request.CountryCode))
            {
                return SendFail(
                    "Country code is required.");
            }

            // A soft-deleted leftover with this mobile/email must not block re-registration.
            await AccountCleanupService.PurgeSoftDeletedCandidatesAsync(
                _context,
                isEmail ? identifier : null,
                isEmail ? null : identifier);

            if (isEmail)
            {
                var existingEmail =
                    await _context.Users.AnyAsync(x =>
                        x.Email != null &&
                        x.Email.ToLower() == identifier);

                if (existingEmail)
                {
                    return SendFail(
                        "Email already registered.");
                }
            }
            else
            {
                var existingMobile =
                    await _context.Users.AnyAsync(x =>
                        x.MobileNumber == identifier &&
                        x.CountryCode == request.CountryCode);

                if (existingMobile)
                {
                    return SendFail(
                        "Mobile number already registered.");
                }
            }

            // Remove previous unverified OTPs
            var oldOtps =
                await _context.OtpVerifications
                .Where(x =>
                    x.MobileNumber == identifier &&
                    !x.IsVerified &&
                    x.Purpose == "CandidateRegistration")
                .ToListAsync();

            if (oldOtps.Any())
            {
                _context.OtpVerifications.RemoveRange(oldOtps);

                await _context.SaveChangesAsync();
            }

            if (isEmail)
            {
                var otpCode = GenerateOtp();

                var otpRecord =
                    new OtpVerification
                    {
                        OtpId = Guid.NewGuid(),
                        UserId = null,
                        MobileNumber = identifier,
                        CountryCode = "email",
                        OtpCode = BCrypt.Net.BCrypt.HashPassword(otpCode),
                        OtpSentAt = DateTime.UtcNow,
                        OtpExpiresAt = DateTime.UtcNow.AddMinutes(
                            OtpExpiryMinutes),
                        ResendCooldownSec = ResendCooldownSeconds,
                        OtpAttempts = 0,
                        IsVerified = false,
                        Purpose = "CandidateRegistration"
                    };

                _context.OtpVerifications.Add(
                    otpRecord);

                await _context.SaveChangesAsync();

                // ===== QA BYPASS: real email OTP send disabled =====
                await _emailService.SendOtpEmailAsync(
                    identifier,
                    otpCode);
                //_logger.LogInformation(
                //    "QA BYPASS - Email OTP send skipped. Static OTP 123456 applies.");
                // ===== END QA BYPASS =====
            }
            else
            {
                var phoneNumber =
                    $"{request.CountryCode}{identifier}";

                _logger.LogInformation(
                    "REGISTRATION OTP SEND - Phone:{Phone}",
                    phoneNumber);

                // ===== QA BYPASS: real Twilio OTP send disabled =====
                // Review account (see OtpBypass): skip the SMS entirely.
                var sent =
                    OtpBypass.IsRegistrationBypass(request.CountryCode, identifier)
                    || await _twilioOtpService
                        .SendOtpAsync(phoneNumber);
                //var sent = true;
                // ===== END QA BYPASS =====

                _logger.LogInformation(
                    "TWILIO RESULT - Sent:{Sent}",
                    sent);

                if (!sent)
                {
                    return SendFail(
                        "Unable to send OTP.");
                }

                var otpRecord =
                    new OtpVerification
                    {
                        OtpId = Guid.NewGuid(),
                        UserId = null,
                        MobileNumber = identifier,
                        CountryCode = request.CountryCode,
                        OtpCode = "TWILIO_VERIFY",
                        OtpSentAt = DateTime.UtcNow,
                        OtpExpiresAt = DateTime.UtcNow.AddMinutes(
                            OtpExpiryMinutes),
                        ResendCooldownSec = ResendCooldownSeconds,
                        OtpAttempts = 0,
                        IsVerified = false,
                        Purpose = "CandidateRegistration"
                    };

                _context.OtpVerifications.Add(
                    otpRecord);

                await _context.SaveChangesAsync();

                // Review account (see OtpBypass): no OTP screen at all. Verify
                // here and hand the one-time registration token straight back,
                // so the app proceeds as if the OTP had been entered.
                if (OtpBypass.IsRegistrationBypass(request.CountryCode, identifier))
                {
                    var auth = await VerifyOtpAsync(
                        new CandidateVerifyOtpRequestDto
                        {
                            Identifier = identifier,
                            CountryCode = request.CountryCode,
                            OtpCode = "000000"
                        },
                        ipAddress);

                    if (!auth.Success || string.IsNullOrEmpty(auth.OtpToken))
                    {
                        return SendFail(auth.Message);
                    }

                    return new SendOtpResponseDto
                    {
                        Success = true,
                        Message = "Mobile number verified successfully.",
                        MaskedIdentifier = MaskMobile(identifier),
                        IdentifierType = "mobile",
                        OtpSkipped = true,
                        OtpToken = auth.OtpToken
                    };
                }
            }

            return new SendOtpResponseDto
            {
                Success = true,
                Message = "OTP sent successfully.",
                MaskedIdentifier =
                    isEmail
                        ? MaskEmail(identifier)
                        : MaskMobile(identifier),
                IdentifierType =
                    isEmail
                        ? "email"
                        : "mobile",
                ExpiresInSeconds =
                    OtpExpiryMinutes * 60,
                ResendCooldownSeconds =
                    ResendCooldownSeconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Registration OTP error. IP:{IP}",
                ipAddress);

            return SendFail(
                "We couldn't send the OTP right now. Please try again in a few minutes.");
        }
    }

    // =====================================================
    // SEND OTP
    // =====================================================

    //    public async Task<SendOtpResponseDto> SendOtpAsync(
    //    CandidateSendOtpRequestDto request,
    //    string ipAddress)
    //    {
    //        try
    //        {
    //            var identifier =
    //            request.Identifier.Trim().ToLower();

    //    var isEmail = IsEmail(identifier);

    //            User? user;

    //            if (isEmail)
    //            {
    //                user = await _context.Users
    //                    .FirstOrDefaultAsync(x =>
    //                        x.Email != null &&
    //                        x.Email.ToLower() == identifier &&
    //                        x.UserType == UserType.Candidate);
    //            }
    //            else
    //            {
    //                user = await _context.Users
    //                    .FirstOrDefaultAsync(x =>
    //                        x.MobileNumber == identifier &&
    //                        x.CountryCode == request.CountryCode &&
    //                        x.UserType == UserType.Candidate);
    //            }

    //            if (user == null)
    //            {
    //                return SendFail(
    //                    "Candidate account not found.");
    //            }

    //            // EMAIL OTP
    //            if (isEmail)
    //            {
    //                var otpCode = GenerateOtp();

    //                var otpRecord = new OtpVerification
    //                {
    //                    OtpId = Guid.NewGuid(),
    //                    UserId = user.UserId,
    //                    MobileNumber = identifier,
    //                    CountryCode = "email",
    //                    OtpCode =
    //                        BCrypt.Net.BCrypt.HashPassword(otpCode),
    //                    OtpSentAt = DateTime.UtcNow,
    //                    OtpExpiresAt =
    //                        DateTime.UtcNow.AddMinutes(
    //                            OtpExpiryMinutes),
    //                    ResendCooldownSec =
    //                        ResendCooldownSeconds,
    //                    OtpAttempts = 0,
    //                    IsVerified = false,
    //                    Purpose = "CandidateLogin"
    //                };

    //                _context.OtpVerifications.Add(otpRecord);

    //                await _context.SaveChangesAsync();

    //                await _emailService
    //                    .SendOtpEmailAsync(
    //                        identifier,
    //                        otpCode);
    //            }
    //            // MOBILE OTP
    //            else
    //            {
    //                var phoneNumber =
    //                    $"{request.CountryCode}{identifier}";

    //                var sent =
    //                    await _twilioOtpService
    //                        .SendOtpAsync(phoneNumber);

    //                if (!sent)
    //                {
    //                    return SendFail(
    //                        "Unable to send OTP.");
    //                }

    //                var otpRecord = new OtpVerification
    //                {
    //                    OtpId = Guid.NewGuid(),
    //                    UserId = user.UserId,
    //                    MobileNumber = identifier,
    //                    CountryCode = request.CountryCode,
    //                    OtpCode = "TWILIO_VERIFY",
    //                    OtpSentAt = DateTime.UtcNow,
    //                    OtpExpiresAt =
    //                        DateTime.UtcNow.AddMinutes(
    //                            OtpExpiryMinutes),
    //                    ResendCooldownSec =
    //                        ResendCooldownSeconds,
    //                    OtpAttempts = 0,
    //                    IsVerified = false,
    //                    Purpose = "CandidateLogin"
    //                };

    //                _context.OtpVerifications.Add(
    //                    otpRecord);

    //                await _context.SaveChangesAsync();
    //            }

    //            return new SendOtpResponseDto
    //            {
    //                Success = true,
    //                Message = "OTP sent successfully.",
    //                MaskedIdentifier =
    //                    isEmail
    //                        ? MaskEmail(identifier)
    //                        : MaskMobile(identifier),
    //                IdentifierType =
    //                    isEmail ? "email" : "mobile",
    //                ExpiresInSeconds =
    //                    OtpExpiryMinutes * 60,
    //                ResendCooldownSeconds =
    //                    ResendCooldownSeconds
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(
    //                ex,
    //                "Send OTP Error");

    //            return SendFail(
    //                "Failed to send OTP.");
    //        }

    //}

    // =====================================================
    // VERIFY OTP
    // =====================================================

    public async Task<AuthResponseDto> VerifyOtpAsync(
    CandidateVerifyOtpRequestDto request,
    string ipAddress)
    {
        try
        {
            var identifier =
            request.Identifier.Trim().ToLower();

            var isEmail = IsEmail(identifier);

            if (!isEmail)
            {
                identifier = identifier
                    .Replace(" ", "")
                    .Replace("-", "");
            }

            var otpRecord =
                await _context.OtpVerifications
                .Where(x =>
                    x.MobileNumber == identifier &&
                    !x.IsVerified &&
                    x.Purpose == "CandidateRegistration")
                .OrderByDescending(x => x.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "OTP not found."
                };
            }

            if (DateTime.UtcNow > otpRecord.OtpExpiresAt)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "OTP expired."
                };
            }

            if (otpRecord.OtpAttempts >= MaxOtpAttempts)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message =
                        "Too many failed attempts. Please request a new OTP."
                };
            }

            bool valid;

            // ===== QA BYPASS: static OTP "123456" accepted, real checks disabled =====
            if (isEmail)
            {
                valid =
                    BCrypt.Net.BCrypt.Verify(
                        request.OtpCode,
                        otpRecord.OtpCode);
            }
            else
            {
                var phoneNumber =
                    $"{request.CountryCode}{identifier}";

                // Review account (see OtpBypass): any code is accepted.
                valid =
                    OtpBypass.IsRegistrationBypass(request.CountryCode, identifier)
                    || await _twilioOtpService
                        .VerifyOtpAsync(
                            phoneNumber,
                            request.OtpCode);
            }
            //valid = request.OtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!valid)
            {
                otpRecord.OtpAttempts++;

                await _context.SaveChangesAsync();

                var remainingAttempts =
                    MaxOtpAttempts - otpRecord.OtpAttempts;

                return new AuthResponseDto
                {
                    Success = false,
                    Message =
                        remainingAttempts > 0
                            ? $"Invalid OTP. {remainingAttempts} attempt(s) remaining."
                            : "Too many failed attempts. Please request a new OTP."
                };
            }

            // Generate one-time OTP token
            var otpToken =
                Guid.NewGuid().ToString();

            otpRecord.IsVerified = true;

            otpRecord.VerificationToken =
                otpToken;

            otpRecord.VerifiedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                Message = "OTP verified successfully.",
                OtpToken = otpToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Verify OTP Error. IP:{IP}",
                ipAddress);

            return new AuthResponseDto
            {
                Success = false,
                Message =
                    "An error occurred while verifying OTP."
            };
        }

    }


    public async Task<CreateCandidateOrderResponseDto> CreateOrderAsync(
        CreateCandidateOrderRequestDto request)
    {
        try
        {
            // ------------------------------------------------------------
            // 1. Resolve active Candidate membership plan
            // ------------------------------------------------------------
            var plan = await ResolveCandidateMembershipPlanAsync(request.Region);

            if (plan == null)
            {
                return new CreateCandidateOrderResponseDto
                {
                    Success = false,
                    Message = "Candidate membership is not available right now. Please try again later."
                };
            }

            if (plan.Price <= 0)
            {
                return new CreateCandidateOrderResponseDto
                {
                    Success = false,
                    Message = "This membership plan doesn't require payment."
                };
            }

            // ------------------------------------------------------------
            // 2. Original plan amount
            // ------------------------------------------------------------
            var originalAmount = plan.Price;

            decimal discountAmount = 0;
            decimal finalAmount = originalAmount;
            Guid? couponId = null;
            string? appliedCouponCode = null;

            // ------------------------------------------------------------
            // 3. Validate coupon if supplied
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var couponValidationRequest = new ValidateCouponRequestDto
                {
                    Code = request.CouponCode.Trim(),
                    PlanType = PlanType.Candidate,
                    PlanId = plan.PlanId,
                    Region = string.IsNullOrWhiteSpace(request.Region)
                        ? "in"
                        : request.Region.Trim().ToLowerInvariant()
                };

                var couponResult = await _couponValidationService
                    .ValidateCouponAsync(
                        couponValidationRequest,
                        Guid.Empty);

                if (!couponResult.IsValid)
                {
                    return new CreateCandidateOrderResponseDto
                    {
                        Success = false,
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        Amount = originalAmount,
                        Currency = "INR",
                        Message = couponResult.Message
                    };
                }

                discountAmount = couponResult.DiscountAmount;
                finalAmount = couponResult.FinalAmount;
                couponId = couponResult.CouponId;
                appliedCouponCode = couponResult.CouponCode;
            }

            // ------------------------------------------------------------
            // 4. Final amount must still be valid
            // ------------------------------------------------------------
            if (finalAmount <= 0)
            {
                return new CreateCandidateOrderResponseDto
                {
                    Success = false,
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    Amount = originalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    Currency = "INR",
                    CouponCode = appliedCouponCode,
                    Message = "The final payment amount must be greater than zero."
                };
            }

            // ------------------------------------------------------------
            // 5. Convert final amount to paise
            // ------------------------------------------------------------
            var amountPaise = (int)Math.Round(
                finalAmount * 100,
                MidpointRounding.AwayFromZero);

            var client = new RazorpayClient(
                _config["Razorpay:KeyId"],
                _config["Razorpay:KeySecret"]);

            // ------------------------------------------------------------
            // 6. Create Razorpay order using FINAL discounted amount
            // ------------------------------------------------------------
            var options = new Dictionary<string, object>
        {
            { "amount", amountPaise },
            { "currency", "INR" },
            { "receipt", $"CANDMEM-{plan.PlanId.ToString("N")[..12]}" }
        };

            Order order = client.Order.Create(options);

            // ------------------------------------------------------------
            // 7. Return order + coupon information
            // ------------------------------------------------------------
            return await Task.FromResult(
                new CreateCandidateOrderResponseDto
                {
                    Success = true,

                    OrderId = order["id"].ToString(),

                    // Original plan amount
                    Amount = originalAmount,

                    // Original amount in paise
                    AmountPaise = (int)Math.Round(
                        originalAmount * 100,
                        MidpointRounding.AwayFromZero),

                    // Coupon discount
                    DiscountAmount = discountAmount,

                    DiscountAmountPaise = (int)Math.Round(
                        discountAmount * 100,
                        MidpointRounding.AwayFromZero),

                    // Final amount actually charged by Razorpay
                    FinalAmount = finalAmount,

                    FinalAmountPaise = amountPaise,

                    Currency = "INR",

                    RazorpayKeyId =
                        _config["Razorpay:KeyId"] ?? string.Empty,

                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,

                    CouponCode = appliedCouponCode,

                    Message = "Order created successfully."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateOrder Error");

            return new CreateCandidateOrderResponseDto
            {
                Success = false,
                Message = "Payment gateway error. Please try again."
            };
        }
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private static bool IsEmail(string value)
    {
        return value.Contains("@");
    }

    private static bool IsMobile(string value)
    {
        return value.All(char.IsDigit);
    }

    private static string GenerateOtp()
    {
        return new Random()
            .Next(100000, 999999)
            .ToString();
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');

        if (parts[0].Length < 3)
            return "***@" + parts[1];

        return parts[0].Substring(0, 2)
               + "****@"
               + parts[1];
    }

    private static string MaskMobile(string mobile)
    {
        return "******" + mobile[^4..];
    }

    private static SendOtpResponseDto SendFail(
        string message)
    {
        return new SendOtpResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static CandidateRegisterResponseDto Fail(
        string message)
    {
        return new CandidateRegisterResponseDto
        {
            Success = false,
            Message = message
        };
    }
}