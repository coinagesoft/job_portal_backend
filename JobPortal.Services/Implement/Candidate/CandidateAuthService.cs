using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace JobPortal.Services.Implement.Candidate;

public class CandidateAuthService : ICandidateAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CandidateAuthService> _logger;

    private const int OtpExpiryMinutes = 10;
    private const int ResendCooldownSeconds = 60;

    public CandidateAuthService(
        AppDbContext context,
        JwtService jwtService,
        IEmailService emailService,
        ILogger<CandidateAuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
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

            var existingUser =
                await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.MobileNumber == request.MobileNumber &&
                    x.CountryCode == request.CountryCode);

            if (existingUser != null)
            {
                return Fail(
                    "Mobile number already registered.");
            }

            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserType = UserType.Candidate,
                MobileNumber = request.MobileNumber,
                CountryCode = request.CountryCode,
                Email = request.Email,
                PasswordHash = "OTP_AUTH",
                AccountStatus = AccountStatus.Active,
                KycStatus = KycStatus.Pending,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

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

            await _context.SaveChangesAsync();

            var token =
                _jwtService.GenerateToken(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber);

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

    // =====================================================
    // SEND OTP
    // =====================================================

    public async Task<SendOtpResponseDto> SendOtpAsync(
        CandidateSendOtpRequestDto request,
        string ipAddress)
    {
        try
        {
            var identifier =
                request.Identifier.Trim().ToLower();

            var isEmail = IsEmail(identifier);

            User? user;

            if (isEmail)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email != null &&
                        x.Email.ToLower() == identifier &&
                        x.UserType == UserType.Candidate);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.MobileNumber == identifier &&
                        x.CountryCode == request.CountryCode &&
                        x.UserType == UserType.Candidate);
            }

            if (user == null)
            {
                return SendFail(
                    "Candidate account not found.");
            }

            var otpCode = GenerateOtp();

            var otpRecord = new OtpVerification
            {
                OtpId = Guid.NewGuid(),
                UserId = user.UserId,
                MobileNumber = identifier,
                CountryCode =
                    request.CountryCode ?? "email",
                OtpCode =
                    BCrypt.Net.BCrypt.HashPassword(otpCode),
                OtpSentAt = DateTime.UtcNow,
                OtpExpiresAt =
                    DateTime.UtcNow.AddMinutes(
                        OtpExpiryMinutes),
                ResendCooldownSec =
                    ResendCooldownSeconds,
                OtpAttempts = 0,
                IsVerified = false,
                Purpose = "CandidateLogin"
            };

            _context.OtpVerifications.Add(otpRecord);

            await _context.SaveChangesAsync();

            if (isEmail)
            {
                await _emailService
                    .SendOtpEmailAsync(
                        identifier,
                        otpCode);
            }
            else
            {
                _logger.LogInformation(
                    "Candidate OTP : {Otp}",
                    otpCode);
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
                    isEmail ? "email" : "mobile",
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
                "Send OTP Error");

            return SendFail(
                "Failed to send OTP.");
        }
    }
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

            User? user;

            if (isEmail)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email != null &&
                        x.Email.ToLower() == identifier &&
                        x.UserType == UserType.Candidate);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.MobileNumber == identifier &&
                        x.CountryCode == request.CountryCode &&
                        x.UserType == UserType.Candidate);
            }

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Candidate account not found."
                };
            }

            var otpRecord =
                await _context.OtpVerifications
                .Where(x =>
                    x.UserId == user.UserId &&
                    !x.IsVerified &&
                    x.Purpose == "CandidateLogin")
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

            var valid =
                BCrypt.Net.BCrypt.Verify(
                    request.OtpCode,
                    otpRecord.OtpCode);

            if (!valid)
            {
                otpRecord.OtpAttempts++;

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            otpRecord.IsVerified = true;

            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var token =
                _jwtService.GenerateToken(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber);

            var profile =
                await _context.CandidateProfiles
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.UserId);

            return new AuthResponseDto
            {
                Success = true,

                Message = "Login successful.",

                Token = token,

                UserId = user.UserId,

                UserType = "Candidate",

                UserName = profile?.FullName,

                ProfileStatus =
                    profile?.ProfileCompletionPct >= 70
                        ? "complete"
                        : "incomplete",

                RedirectTo =
                    profile?.ProfileCompletionPct >= 70
                        ? "/candidate/dashboard"
                        : "/candidate/profile/setup"
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
                Message = "An error occurred while verifying OTP."
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