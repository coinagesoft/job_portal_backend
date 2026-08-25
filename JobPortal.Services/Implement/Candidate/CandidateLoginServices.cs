using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JobPortal.Services.Implement.Candidate;

/// <summary>
/// Auth service used ONLY by the candidate mobile app.
/// Every entry point rejects non-candidate accounts (e.g. a recruiter
/// entering their OTP/Google/LinkedIn credentials in the candidate app)
/// with a clear "wrong app" message instead of logging them in.
/// </summary>
public class CandidateLoginService : ICandidateLoginService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<CandidateAuthService> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly ISubUserEmailService _emailService;

    private const int OtpExpiryMinutes = 10;
    private const int MaxOtpAttempts = 3;
    private const int ResendCooldownSeconds = 30;
    private const string WrongAppMessage =
        "This app is for candidates only. Please use the recruiter portal to log in.";

    public CandidateLoginService(
        AppDbContext context,
        JwtService jwtService,
        ISubUserEmailService emailService,
        ILogger<CandidateAuthService> logger,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ITwilioOtpService twilioOtpService)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _twilioOtpService = twilioOtpService;
    }

    // ════════════════════════════════════════════════
    // SEND OTP
    // ════════════════════════════════════════════════
    public async Task<SendOtpResponseDto> SendOtpAsync(
        SendOtpRequestDto request,
        string ipAddress)
    {
        try
        {
            var identifier = request.Identifier.Trim().ToLower();
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

            _logger.LogInformation(
                "CANDIDATE SEND OTP START - Identifier:{Identifier}",
                request.Identifier);

            if (!isEmail && !isMobile)
            {
                return SendFail("Please enter a valid email or mobile number.");
            }

            if (isMobile && string.IsNullOrWhiteSpace(request.CountryCode))
            {
                return SendFail("Country code is required for mobile number.");
            }

            User? user;

            if (isEmail)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() == identifier);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.MobileNumber == identifier &&
                        u.CountryCode == request.CountryCode);
            }

            if (user == null)
            {
                return SendFail("No account found. Please register first.");
            }

            // ── Candidate-app gate ──────────────────────────
            if (user.UserType != UserType.Candidate)
            {
                _logger.LogWarning(
                    "CANDIDATE APP - blocked non-candidate login attempt. UserId:{UserId} Type:{Type} IP:{IP}",
                    user.UserId, user.UserType, ipAddress);

                return SendFail(WrongAppMessage);
            }

            if (user.AccountStatus == AccountStatus.Suspended)
            {
                return SendFail("Your account has been suspended. Contact support.");
            }

            if (user.AccountStatus == AccountStatus.Rejected)
            {
                return SendFail("Account not found.");
            }

            var activeOtps = await _context.OtpVerifications
                .Where(o =>
                    o.MobileNumber == identifier &&
                    o.Purpose == "CandidateLogin" &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .ToListAsync();

            var recentOtp = activeOtps.FirstOrDefault();

            if (recentOtp != null)
            {
                var cooldownEnd =
                    recentOtp.OtpSentAt.AddSeconds(recentOtp.ResendCooldownSec);

                if (DateTime.UtcNow < cooldownEnd)
                {
                    var waitSecs = Math.Max(
                        1,
                        (int)(cooldownEnd - DateTime.UtcNow).TotalSeconds);

                    return SendFail(
                        $"Please wait {waitSecs} seconds before requesting a new OTP.");
                }

                foreach (var otp in activeOtps)
                {
                    otp.IsVerified = true;
                }

                await _context.SaveChangesAsync();
            }

            if (isEmail)
            {
                var otpCode = GenerateOtp();

                var otpRecord = new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = identifier,
                    CountryCode = "email",
                    OtpCode = BCrypt.Net.BCrypt.HashPassword(otpCode),
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                    ResendCooldownSec = ResendCooldownSeconds,
                    IsVerified = false,
                    Purpose = "CandidateLogin"
                };

                _context.OtpVerifications.Add(otpRecord);
                await _context.SaveChangesAsync();

                // ===== QA BYPASS: real email OTP send disabled =====
                await _emailService.SendOtpEmailAsync(identifier, otpCode);
                //_logger.LogInformation(
                //    "QA BYPASS - Candidate Login Email OTP send skipped. Static OTP 123456 applies.");
                // ===== END QA BYPASS =====
            }
            else
            {
                var phoneNumber = $"{request.CountryCode}{identifier}";

                _logger.LogInformation(
                    "TWILIO SEND OTP - Phone:{Phone}", phoneNumber);

                // ===== QA BYPASS: real Twilio OTP send disabled =====
                var sent = await _twilioOtpService.SendOtpAsync(phoneNumber);
                //var sent = true;
                // ===== END QA BYPASS =====

                if (!sent)
                {
                    return SendFail("Unable to send OTP.");
                }

                var otpRecord = new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = identifier,
                    CountryCode = request.CountryCode,
                    OtpCode = "TWILIO_VERIFY",
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                    ResendCooldownSec = ResendCooldownSeconds,
                    IsVerified = false,
                    Purpose = "CandidateLogin"
                };

                _context.OtpVerifications.Add(otpRecord);
                await _context.SaveChangesAsync();
            }

            var masked = isEmail ? MaskEmail(identifier) : MaskMobile(identifier);

            return new SendOtpResponseDto
            {
                Success = true,
                Message = $"OTP sent to {masked}. Valid for {OtpExpiryMinutes} minutes.",
                MaskedIdentifier = masked,
                IdentifierType = isEmail ? "email" : "mobile",
                ExpiresInSeconds = OtpExpiryMinutes * 30,
                ResendCooldownSeconds = ResendCooldownSeconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate SendOtp error. IP:{IP}", ipAddress);
            return SendFail("We couldn't send the OTP right now. Please try again in a few minutes.");
        }
    }

    // ════════════════════════════════════════════════
    // VERIFY OTP
    // ════════════════════════════════════════════════
    public async Task<AuthResponseDto> VerifyOtpAsync(
        VerifyOtpRequestDto request,
        string ipAddress)
    {
        try
        {
            var identifier = request.Identifier.Trim().ToLower();
            var isEmail = IsEmail(identifier);

            if (!isEmail)
            {
                identifier = identifier
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");
            }

            User? user;

            if (isEmail)
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email != null &&
                        u.Email.ToLower() == identifier);
            }
            else
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.MobileNumber == identifier &&
                        u.CountryCode == request.CountryCode);
            }

            if (user == null)
                return AuthFail("Account not found.");

            // ── Candidate-app gate ──────────────────────────
            if (user.UserType != UserType.Candidate)
            {
                _logger.LogWarning(
                    "CANDIDATE APP - blocked non-candidate verify attempt. UserId:{UserId} Type:{Type} IP:{IP}",
                    user.UserId, user.UserType, ipAddress);

                return AuthFail(WrongAppMessage);
            }

            if (user.AccountStatus == AccountStatus.Suspended)
                return AuthFail("Your account has been suspended.");

            if (user.AccountStatus == AccountStatus.Rejected)
                return AuthFail("Account not found.");

            var otp = await _context.OtpVerifications
                .Where(o =>
                    o.UserId == user.UserId &&
                    o.Purpose == "CandidateLogin" &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return AuthFail("OTP not found. Please request a new OTP.");

            if (DateTime.UtcNow > otp.OtpExpiresAt)
                return AuthFail("OTP has expired. Please request a new one.");

            if (otp.OtpAttempts >= MaxOtpAttempts)
                return AuthFail("Too many failed attempts. Please request a new OTP.");

            bool isValid;

            // ===== QA BYPASS: static OTP "123456" accepted, real checks disabled =====
            if (isEmail)
            {
                isValid = BCrypt.Net.BCrypt.Verify(request.OtpCode, otp.OtpCode);
            }
            else
            {
                var phoneNumber = $"{request.CountryCode}{identifier}";
                isValid = await _twilioOtpService.VerifyOtpAsync(phoneNumber, request.OtpCode);
            }
            //isValid = request.OtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!isValid)
            {
                otp.OtpAttempts++;
                await _context.SaveChangesAsync();

                var remaining = MaxOtpAttempts - otp.OtpAttempts;

                return AuthFail(
                    remaining > 0
                        ? $"Invalid OTP. {remaining} attempt(s) remaining."
                        : "Too many failed attempts. Please request a new OTP.");
            }

            otp.IsVerified = true;

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            if (user.AccountStatus == AccountStatus.Pending)
            {
                user.AccountStatus = AccountStatus.Active;
            }

            await _context.SaveChangesAsync();

            var (token, expiry, candidateId) = await GenerateCandidateTokenAsync(user);
            var profileStatus = await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "Candidate login success — UserId:{UserId} IP:{IP}",
                user.UserId, ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                CandidateId = candidateId,
                UserId = user.UserId,
                UserType = user.UserType.ToString(),
                UserName = await GetUserNameAsync(user),
                ProfileStatus = profileStatus,
                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate VerifyOtp error. IP:{IP}", ipAddress);
            return AuthFail("We couldn't verify that OTP right now. Please try again.");
        }
    }

    // ════════════════════════════════════════════════
    // GOOGLE LOGIN
    // ════════════════════════════════════════════════
    public async Task<AuthResponseDto> GoogleLoginAsync(
        GoogleLoginRequestDto request,
        string ipAddress)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.AccessToken);

            var googleResponse = await httpClient.GetAsync(
                "https://www.googleapis.com/oauth2/v3/userinfo");

            if (!googleResponse.IsSuccessStatusCode)
            {
                return AuthFail("Invalid Google session.");
            }

            var googleJson = await googleResponse.Content.ReadAsStringAsync();
            using var googleDoc = JsonDocument.Parse(googleJson);

            var email = googleDoc.RootElement
                .GetProperty("email").GetString()?.ToLower();

            var name = googleDoc.RootElement
                .GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthFail("Google account email not found.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                // Auto-register Candidate — this is the only case where
                // a brand-new account is created from this app.
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = "",
                    PasswordHash = "GOOGLE_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "New candidate registered via Google - UserId:{Id}", user.UserId);
            }
            else
            {
                // ── Candidate-app gate ──────────────────────
                if (user.UserType != UserType.Candidate)
                {
                    _logger.LogWarning(
                        "CANDIDATE APP - blocked non-candidate Google login. UserId:{UserId} Type:{Type} IP:{IP}",
                        user.UserId, user.UserType, ipAddress);

                    return AuthFail(WrongAppMessage);
                }

                if (user.AccountStatus == AccountStatus.Suspended)
                    return AuthFail("Your account has been suspended.");

                if (user.AccountStatus == AccountStatus.Rejected)
                    return AuthFail("Account has been rejected.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var (token, expiry, candidateId) = await GenerateCandidateTokenAsync(user);
            var profileStatus = await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "Candidate Google login - UserId:{Id} IP:{IP}", user.UserId, ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Google login successful.",
                Token = token,
                UserId = user.UserId,
                CandidateId = candidateId,
                UserType = user.UserType.ToString(),
                UserName = name,
                ProfileStatus = profileStatus,
                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate Google login error. IP:{IP}", ipAddress);
            return AuthFail("An error occurred. Please try again.");
        }
    }

    // ════════════════════════════════════════════════
    // LINKEDIN LOGIN
    // ════════════════════════════════════════════════
    public async Task<AuthResponseDto> LinkedInLoginAsync(
        LinkedInLoginRequestDto request,
        string ipAddress)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var tokenResponse = await httpClient.PostAsync(
                "https://www.linkedin.com/oauth/v2/accessToken",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "authorization_code",
                        ["code"] = request.LinkedInCode,
                        ["redirect_uri"] = request.RedirectUri,
                        ["client_id"] = _config["LinkedIn:ClientId"]!,
                        ["client_secret"] = _config["LinkedIn:ClientSecret"]!
                    }));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "LinkedIn token exchange failed. IP:{IP}", ipAddress);

                return AuthFail("LinkedIn authentication failed. Please try again.");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenJson);

            var accessToken = tokenDoc.RootElement
                .GetProperty("access_token").GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AuthFail("LinkedIn authentication failed.");
            }

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var profileResponse = await httpClient.GetAsync(
                "https://api.linkedin.com/v2/userinfo");

            if (!profileResponse.IsSuccessStatusCode)
            {
                return AuthFail("Failed to get LinkedIn profile.");
            }

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var profileDoc = JsonDocument.Parse(profileJson);

            var email = profileDoc.RootElement.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()?.ToLower()
                : null;

            var name = profileDoc.RootElement.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthFail(
                    "LinkedIn account email not found. Please ensure your LinkedIn email is visible.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = "",
                    CountryCode = "+91",
                    PasswordHash = "LINKEDIN_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "New candidate registered via LinkedIn - UserId:{Id}", user.UserId);
            }
            else
            {
                // ── Candidate-app gate ──────────────────────
                if (user.UserType != UserType.Candidate)
                {
                    _logger.LogWarning(
                        "CANDIDATE APP - blocked non-candidate LinkedIn login. UserId:{UserId} Type:{Type} IP:{IP}",
                        user.UserId, user.UserType, ipAddress);

                    return AuthFail(WrongAppMessage);
                }

                if (user.AccountStatus == AccountStatus.Suspended)
                {
                    return AuthFail("Your account has been suspended. Contact support.");
                }

                if (user.AccountStatus == AccountStatus.Rejected)
                {
                    return AuthFail("Account has been rejected.");
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var (token, expiry, candidateId) = await GenerateCandidateTokenAsync(user);
            var profileStatus = await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "Candidate LinkedIn login - UserId:{Id} IP:{IP}", user.UserId, ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "LinkedIn login successful.",
                Token = token,
                UserId = user.UserId,
                CandidateId = candidateId,
                UserType = user.UserType.ToString(),
                UserName = name,
                ProfileStatus = profileStatus,
                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Candidate LinkedIn login error. IP:{IP}", ipAddress);
            return AuthFail("An error occurred. Please try again.");
        }
    }

    // ── Private Helpers ───────────────────────────────────

    private async Task<(string token, DateTime expiry, Guid? candidateId)> GenerateCandidateTokenAsync(User user)
    {
        var candidateId = await _context.CandidateProfiles
            .Where(x => x.UserId == user.UserId)
            .Select(x => (Guid?)x.CandidateId)
            .FirstOrDefaultAsync();

        var (token, expiry) = await _jwtService.GenerateTokenAsync(
            user.UserId,
            user.UserType.ToString(),
            user.MobileNumber,
            null,          // employerId — never applicable in the candidate app
            candidateId,
            false);        // isSubUser — never applicable in the candidate app

        return (token, expiry, candidateId);
    }

    private async Task<string> GetProfileStatusAsync(User user)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.UserId);

        return profile?.ProfileCompletionPct >= 70
            ? "complete"
            : "incomplete";
    }

    private async Task<string?> GetUserNameAsync(User user)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.UserId);

        return profile?.FullName;
    }

    private static bool IsEmail(string s) =>
        s.Contains('@') && s.Contains('.');

    private static bool IsMobile(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim()
             .Replace(" ", "")
             .Replace("-", "")
             .Replace("(", "")
             .Replace(")", "");

        return s.All(char.IsDigit)
            && s.Length >= 7
            && s.Length <= 12;
    }

    private static string GenerateOtp()
    {
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        var name = parts[0];
        var masked = name.Length <= 2
            ? name[0] + "***"
            : name[..2] + new string('*', name.Length - 2);
        return $"{masked}@{parts[1]}";
    }

    private static string MaskMobile(string mobile) =>
        mobile.Length <= 4
            ? "****"
            : new string('*', mobile.Length - 4) + mobile[^4..];

    private static SendOtpResponseDto SendFail(string message) =>
        new() { Success = false, Message = message };

    private static AuthResponseDto AuthFail(string message) =>
        new() { Success = false, Message = message };
}