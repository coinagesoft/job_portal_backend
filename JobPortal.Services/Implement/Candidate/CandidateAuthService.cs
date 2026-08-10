using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razorpay.Api;


namespace JobPortal.Services.Implement.Candidate;

public class CandidateAuthService : ICandidateAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CandidateAuthService> _logger;
    private readonly ITwilioOtpService _twilioOtpService;
    private const int OtpExpiryMinutes = 10;
    private readonly IConfiguration _config;

    private const int ResendCooldownSeconds = 30;

    private const int MaxOtpAttempts = 5;


    public CandidateAuthService(
        AppDbContext context,
        JwtService jwtService,
        IEmailService emailService,
        ITwilioOtpService twilioOtpService,
         IConfiguration config,
        ILogger<CandidateAuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
        _config = config;
        _twilioOtpService = twilioOtpService;
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
            if (!request.TermsAccepted)
            {
                return Fail(
                "Terms and Conditions must be accepted.");
            }

            // Verify OTP token
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

            // Payment validation
            if (string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
                string.IsNullOrWhiteSpace(request.RazorpaySignature))
            {
                return Fail("Payment verification failed.");
            }

            var paymentVerified = VerifyRazorpaySignature(
                request.RazorpayOrderId,
                request.RazorpayPaymentId,
                request.RazorpaySignature
            );

            if (!paymentVerified)
            {
                return Fail("Payment verification failed.");
            }

            // Mobile already registered?
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

            // Email already registered?
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

            // Create user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserType = UserType.Candidate,
                MobileNumber = request.MobileNumber,
                CountryCode = request.CountryCode,
                // uq_users_email is a plain unique index with no filter for
                // blank values, so an empty string collides with any other
                // candidate who also skipped email — store null instead,
                // which the index correctly allows to repeat.
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

            _context.Users.Add(user);

            // Create profile
            var profile = new CandidateProfile
            {
                CandidateId = Guid.NewGuid(),
                UserId = user.UserId,
                FullName = request.FullName,
                ProfileStatus = "Incomplete",
                ProfileCompletionPct = 0,
                AvailabilityStatus = "Available",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CandidateProfiles.Add(profile);
            // --------------------------------------------------
            // STORE CANDIDATE REGISTRATION PAYMENT
            // --------------------------------------------------

            var paymentTransaction = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),

                UserId = user.UserId,

                CandidateId = profile.CandidateId,

                TransactionType = "CandidateRegistration",

                PackType = null,

                CreditQuantity = null,

                ValidityMonths = null,

                // ₹100 = 10000 paise
                AmountPaise = 100,

                GstAmountPaise = 0,

                TotalAmountPaise = 100,

                PaymentMethod = "Razorpay",

                RazorpayOrderId = request.RazorpayOrderId,

                RazorpayPaymentId = request.RazorpayPaymentId,

                PaymentStatus = "Completed",

                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(paymentTransaction);

            // Consume OTP token (prevent reuse)
            verifiedOtp.VerificationToken = null;

            await _context.SaveChangesAsync();

            // Generate JWT
            var token =
                _jwtService.GenerateToken(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber,
                    candidateId: profile.CandidateId);

            return new CandidateRegisterResponseDto
            {
                Success = true,
                Token = token,
                CandidateId = profile.CandidateId,
                UserName = profile.FullName,
                RedirectTo = "/candidate/profile/setup",
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
                var sent =
                    await _twilioOtpService
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
                $"{ex.GetType().Name}: {ex.Message}");
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

                valid =
                    await _twilioOtpService
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

            _logger.LogInformation(
    "KeyId:{KeyId}",
    _config["Razorpay:KeyId"]);

            _logger.LogInformation(
                "KeySecret:{KeySecret}",
                _config["Razorpay:KeySecret"]);

            var client = new RazorpayClient(
            _config["Razorpay:KeyId"],
            _config["Razorpay:KeySecret"]);

            var options = new Dictionary<string, object>
        {
            { "amount", request.Amount * 100 }, // paisa
            { "currency", "INR" },
            { "receipt", Guid.NewGuid().ToString() }
        };

            Order order = client.Order.Create(options);

            return await Task.FromResult(
                new CreateCandidateOrderResponseDto
                {
                    Success = true,
                    OrderId = order["id"].ToString(),
                    Amount = request.Amount,
                    Currency = "INR",
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
                Message = ex.ToString()
            };
        }
    }

    private bool VerifyRazorpaySignature(
        string orderId,
        string paymentId,
        string signature)
    {
        try
        {
            var attributes = new Dictionary<string, string>
        {
            { "razorpay_order_id", orderId },
            { "razorpay_payment_id", paymentId },
            { "razorpay_signature", signature }
        };

            Utils.verifyPaymentSignature(attributes);

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