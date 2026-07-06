using Google.Apis.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;


namespace JobPortal.Services.Implement;

public class RecruiterAuthService : IRecruiterAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<RecruiterAuthService> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly ISubUserEmailService _emailService;
    private const int OtpExpiryMinutes = 10;
    private const int MaxOtpAttempts = 3;
    private const int ResendCooldownSeconds = 60;

    public RecruiterAuthService(
        AppDbContext context,
        JwtService jwtService,
        ISubUserEmailService emailService,
        ILogger<RecruiterAuthService> logger,
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
                "SEND OTP START - Identifier:{Identifier}",
                request.Identifier);

            //var isEmail = IsEmail(identifier);
            //var isMobile = IsMobile(identifier);

            if (!isEmail && !isMobile)
            {
                return SendFail(
                    "Please enter a valid email or mobile number.");
            }

            if (isMobile &&
                string.IsNullOrWhiteSpace(request.CountryCode))
            {
                return SendFail(
                    "Country code is required for mobile number.");
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
                return SendFail(
                    "No account found. Please register first.");
            }

            var userType = user.UserType;

            if (user.AccountStatus == AccountStatus.Suspended)
            {
                return SendFail(
                    "Your account has been suspended. Contact support.");
            }

            if (user.AccountStatus == AccountStatus.Rejected)
            {
                return SendFail(
                    "Account not found.");
            }

            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.UserId);

            if (employer != null)
            {
                if (employer.AccountStatus == AccountStatus.Suspended)
                {
                    return SendFail(
                        "Company account suspended.");
                }

                if (employer.AccountStatus == AccountStatus.Rejected)
                {
                    return SendFail(
                        "Company account rejected.");
                }
            }

            var activeOtps = await _context.OtpVerifications
                .Where(o =>
                    o.MobileNumber == identifier &&
                    o.Purpose == $"{userType}Login" &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .ToListAsync();

            var recentOtp = activeOtps.FirstOrDefault();

            if (recentOtp != null)
            {
                var cooldownEnd =
                    recentOtp.OtpSentAt.AddSeconds(
                        recentOtp.ResendCooldownSec);

                if (DateTime.UtcNow < cooldownEnd)
                {
                    var waitSecs = Math.Max(
                        1,
                        (int)(cooldownEnd - DateTime.UtcNow)
                        .TotalSeconds);

                    return SendFail(
                        $"Please wait {waitSecs} seconds before requesting a new OTP.");
                }

                foreach (var otp in activeOtps)
                {
                    otp.IsVerified = true;
                }

                await _context.SaveChangesAsync();
            }

            // SEND OTP
            if (isEmail)
            {
                var otpCode = GenerateOtp();

                var otpRecord = new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = identifier,
                    CountryCode = "email",

                    OtpCode = BCrypt.Net.BCrypt.HashPassword(
                        otpCode),

                    OtpSentAt = DateTime.UtcNow,

                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(
                        OtpExpiryMinutes),

                    ResendCooldownSec = ResendCooldownSeconds,

                    IsVerified = false,

                    Purpose = $"{userType}Login"
                };

                _context.OtpVerifications.Add(
                    otpRecord);

                await _context.SaveChangesAsync();

                // ===== QA BYPASS: real email OTP send disabled =====
                // await _emailService.SendOtpEmailAsync(
                //     identifier,
                //     otpCode);
                _logger.LogInformation(
                    "QA BYPASS - Login Email OTP send skipped. Static OTP 123456 applies.");
                // ===== END QA BYPASS =====
            }
            else
            {
                var phoneNumber =
                    $"{request.CountryCode}{identifier}";

                _logger.LogInformation(
                    "TWILIO SEND OTP - Phone:{Phone}",
                    phoneNumber);

                // ===== QA BYPASS: real Twilio OTP send disabled =====
                // var sent =
                //     await _twilioOtpService
                //         .SendOtpAsync(phoneNumber);
                var sent = true;
                // ===== END QA BYPASS =====

                _logger.LogInformation(
                    "TWILIO RESULT - Sent:{Sent}",
                    sent);

                if (!sent)
                {
                    return SendFail(
                        "Unable to send OTP.");
                }

                var otpRecord = new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = identifier,
                    CountryCode = request.CountryCode,

                    OtpCode = "TWILIO_VERIFY",

                    OtpSentAt = DateTime.UtcNow,

                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(
                        OtpExpiryMinutes),

                    ResendCooldownSec = ResendCooldownSeconds,

                    IsVerified = false,

                    Purpose = $"{userType}Login"
                };

                _logger.LogInformation(
                    "INSERT OTP RECORD - User:{UserId} Verified:{Verified}",
                    user.UserId,
                    otpRecord.IsVerified);

                _context.OtpVerifications.Add(
                    otpRecord);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "OTP RECORD SAVED - OtpId:{OtpId}",
                    otpRecord.OtpId);
            }

            var masked = isEmail
                ? MaskEmail(identifier)
                : MaskMobile(identifier);

            return new SendOtpResponseDto
            {
                Success = true,

                Message =
                    $"OTP sent to {masked}. Valid for {OtpExpiryMinutes} minutes.",

                MaskedIdentifier = masked,

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
                "SendOtp error. IP:{IP}",
                ipAddress);

            return SendFail(
                $"{ex.GetType().Name}: {ex.Message}");
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

            var isMobile = IsMobile(identifier);

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

            // User account validation
            if (user.AccountStatus == AccountStatus.Suspended)
                return AuthFail("Your account has been suspended.");

            if (user.AccountStatus == AccountStatus.Rejected)
                return AuthFail("Account not found.");

            // Recruiter validation
            Guid? employerId = null;

            if (user.UserType == UserType.Recruiter)
            {
                var employer = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(x =>
                        x.UserId == user.UserId);

                if (employer == null)
                    return AuthFail("Employer profile not found.");

                if (employer.AccountStatus == AccountStatus.Suspended)
                    return AuthFail("Company account suspended.");

                if (employer.AccountStatus == AccountStatus.Rejected)
                    return AuthFail("Company account rejected.");

                employerId = employer.EmployerId;
            }

            _logger.LogInformation(
                "VERIFY OTP START - User:{UserId}",
                user.UserId);

            var otp = await _context.OtpVerifications
                .Where(o =>
                    o.UserId == user.UserId &&
                    o.Purpose == $"{user.UserType}Login" &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .FirstOrDefaultAsync();

            _logger.LogInformation(
                "VERIFY OTP FOUND:{Found}",
                otp != null);

            if (otp == null)
                return AuthFail(
                    "OTP not found. Please request a new OTP.");

            if (DateTime.UtcNow > otp.OtpExpiresAt)
                return AuthFail(
                    "OTP has expired. Please request a new one.");

            if (otp.OtpAttempts >= MaxOtpAttempts)
                return AuthFail(
                    "Too many failed attempts. Please request a new OTP.");

            bool isValid;

            // ===== QA BYPASS: static OTP "123456" accepted, real checks disabled =====
            // if (isEmail)
            // {
            //     isValid =
            //         BCrypt.Net.BCrypt.Verify(
            //             request.OtpCode,
            //             otp.OtpCode);
            // }
            // else
            // {
            //     var phoneNumber =
            //         $"{request.CountryCode}{identifier}";
            //
            //     isValid =
            //         await _twilioOtpService
            //             .VerifyOtpAsync(
            //                 phoneNumber,
            //                 request.OtpCode);
            // }
            isValid = request.OtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!isValid)
            {
                otp.OtpAttempts++;

                await _context.SaveChangesAsync();

                var remaining =
                    MaxOtpAttempts - otp.OtpAttempts;

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

            var (token, expiry) =
                await GenerateUserTokenAsync(user);

            var profileStatus =
                await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "Login success — UserId:{UserId} Type:{Type} IP:{IP}",
                user.UserId,
                user.UserType,
                ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                UserId = user.UserId,
                EmployerId = employerId,
                UserType = user.UserType.ToString(),
                UserName = await GetUserNameAsync(user),
                ProfileStatus = profileStatus,
                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "VerifyOtp error. IP:{IP}",
                ipAddress);

            return AuthFail(
                $"{ex.GetType().Name}: {ex.Message}");
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

            _logger.LogInformation(
    "GOOGLE LOGIN START. AccessToken Null:{Null}",
    string.IsNullOrWhiteSpace(request.AccessToken));

            // Verify Google Token
            var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    request.AccessToken);

            var googleResponse =
                await httpClient.GetAsync(
                    "https://www.googleapis.com/oauth2/v3/userinfo");

            if (!googleResponse.IsSuccessStatusCode)
            {
                return AuthFail(
                    "Invalid Google session.");
            }

            var googleJson =
                await googleResponse.Content
                    .ReadAsStringAsync();

            _logger.LogInformation(
    "GOOGLE USERINFO RESPONSE: {Json}",
    googleJson);

            using var googleDoc =
                JsonDocument.Parse(googleJson);

            var email =
                googleDoc.RootElement
                    .GetProperty("email")
                    .GetString()
                    ?.ToLower();

            var name =
                googleDoc.RootElement
                    .GetProperty("name")
                    .GetString();

            _logger.LogInformation(
    "GOOGLE EMAIL: {Email}",
    email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthFail(
                    "Google account email not found.");
            }



            // Find Existing User
            var user = await _context.Users
     .FirstOrDefaultAsync(u =>
         u.Email != null &&
         u.Email.ToLower() == email);

            if (user == null)
            {
                // Auto-register Candidate
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
                    "New candidate registered via Google - UserId:{Id}",
                    user.UserId);
            }
            else
            {
                if (user.AccountStatus == AccountStatus.Suspended)
                    return AuthFail(
                        "Your account has been suspended.");

                if (user.AccountStatus == AccountStatus.Rejected)
                    return AuthFail(
                        "Account has been rejected.");
            }

            var userType = user.UserType;

           

          

            // Recruiter Validation
            Guid? employerId = null;

            if (user.UserType == UserType.Recruiter)
            {
                var employer =
                    await _context.EmployerProfiles
                        .FirstOrDefaultAsync(x =>
                            x.UserId == user.UserId);

                if (employer == null)
                {
                    return AuthFail(
                        "Employer profile not found.");
                }

                if (employer.AccountStatus == AccountStatus.Suspended)
                {
                    return AuthFail(
                        "Company account suspended.");
                }

                if (employer.AccountStatus == AccountStatus.Rejected)
                {
                    return AuthFail(
                        "Company account rejected.");
                }

                employerId = employer.EmployerId;
            }
           

            // Update Login Time
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate JWT
            var (token, expiry) =
                await GenerateUserTokenAsync(user);

            // Profile Status
            var profileStatus =
                await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "Google login - UserId:{Id} Type:{Type} IP:{IP}",
                user.UserId,
                userType,
                ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Google login successful.",

                Token = token,

                UserId = user.UserId,

                EmployerId = employerId,

                UserType = userType.ToString(),

                UserName = name,

                ProfileStatus = profileStatus,

               

                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Google login error. IP:{IP}",
                ipAddress);

            return AuthFail(
                "An error occurred. Please try again.");
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
            // Step 1: Exchange code for access token
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
                    "LinkedIn token exchange failed. IP:{IP}",
                    ipAddress);

                return AuthFail(
                    "LinkedIn authentication failed. Please try again.");
            }

            var tokenJson =
                await tokenResponse.Content.ReadAsStringAsync();

            using var tokenDoc =
                JsonDocument.Parse(tokenJson);

            var accessToken =
                tokenDoc.RootElement
                    .GetProperty("access_token")
                    .GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AuthFail(
                    "LinkedIn authentication failed.");
            }

            // Step 2: Get LinkedIn User Profile
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            var profileResponse =
                await httpClient.GetAsync(
                    "https://api.linkedin.com/v2/userinfo");

            if (!profileResponse.IsSuccessStatusCode)
            {
                return AuthFail(
                    "Failed to get LinkedIn profile.");
            }

            var profileJson =
                await profileResponse.Content.ReadAsStringAsync();

            using var profileDoc =
                JsonDocument.Parse(profileJson);

            var email =
                profileDoc.RootElement.TryGetProperty(
                    "email",
                    out var emailProp)
                    ? emailProp.GetString()?.ToLower()
                    : null;

            var name =
                profileDoc.RootElement.TryGetProperty(
                    "name",
                    out var nameProp)
                    ? nameProp.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthFail(
                    "LinkedIn account email not found. Please ensure your LinkedIn email is visible.");
            }

            // Find Existing User
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == email);

            if (user == null)
            {
                // Auto Register Candidate
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
                    "New candidate registered via LinkedIn - UserId:{Id}",
                    user.UserId);
            }
            else
            {
                if (user.AccountStatus == AccountStatus.Suspended)
                {
                    return AuthFail(
                        "Your account has been suspended. Contact support.");
                }

                if (user.AccountStatus == AccountStatus.Rejected)
                {
                    return AuthFail(
                        "Account has been rejected.");
                }
            }

            var userType = user.UserType;

            // Recruiter Validation
            Guid? employerId = null;

            if (userType == UserType.Recruiter)
            {
                var employer =
                    await _context.EmployerProfiles
                        .FirstOrDefaultAsync(x =>
                            x.UserId == user.UserId);

                if (employer == null)
                {
                    return AuthFail(
                        "Employer profile not found.");
                }

                if (employer.AccountStatus == AccountStatus.Suspended)
                {
                    return AuthFail(
                        "Company account suspended.");
                }

                if (employer.AccountStatus == AccountStatus.Rejected)
                {
                    return AuthFail(
                        "Company account rejected.");
                }

                employerId = employer.EmployerId;
            }

            // Update Login Information
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate JWT
            var (token, expiry) =
                await GenerateUserTokenAsync(user);

            // Profile Status
            var profileStatus =
                await GetProfileStatusAsync(user);

            _logger.LogInformation(
                "LinkedIn login - UserId:{Id} Type:{Type} IP:{IP}",
                user.UserId,
                userType,
                ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "LinkedIn login successful.",

                Token = token,

                UserId = user.UserId,

                EmployerId = employerId,

                UserType = userType.ToString(),

                UserName = name,

                ProfileStatus = profileStatus,

               

                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "LinkedIn login error. IP:{IP}",
                ipAddress);

            return AuthFail(
                "An error occurred. Please try again.");
        }
    }
    // ── Private Helpers ───────────────────────────────────

    private async Task<(string token, DateTime expiry)> GenerateUserTokenAsync(User user)
    {
        Guid? employerId = null;
        Guid? candidateId = null;

        if (user.UserType == UserType.Recruiter)
        {
            employerId = await _context.EmployerProfiles
                .Where(x => x.UserId == user.UserId)
                .Select(x => (Guid?)x.EmployerId)
                .FirstOrDefaultAsync();
        }

        if (user.UserType == UserType.Candidate)
        {
            candidateId = await _context.CandidateProfiles
                .Where(x => x.UserId == user.UserId)
                .Select(x => (Guid?)x.CandidateId)
                .FirstOrDefaultAsync();
        }

        var token = _jwtService.GenerateToken(
            user.UserId,
            user.UserType.ToString(),
            user.MobileNumber,
            employerId,
            candidateId);

        return (token, _jwtService.GetExpiry());
    }
    private async Task<string> GetProfileStatusAsync(User user)
    {
        if (user.UserType == UserType.Candidate)
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.UserId);
            return profile?.ProfileCompletionPct >= 70
                ? "complete"
                : "incomplete";
        }

        if (user.UserType == UserType.Recruiter)
        {
            var profile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.UserId);
            return profile?.ProfileCompletionScore >= 70
                ? "complete"
                : "incomplete";
        }

        return "complete";
    }

    private async Task<string?> GetUserNameAsync(User user)
    {
        if (user.UserType == UserType.Candidate)
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.UserId);
            return profile?.FullName;
        }

        if (user.UserType == UserType.Recruiter)
        {
            var profile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.UserId);
            return profile?.ContactPersonName;
        }

        return null;
    }

    private static string GetRedirectUrl(User user, string profileStatus)
    {
        if (user.UserType == UserType.Candidate)
            return profileStatus == "incomplete"
                ? "/candidate/complete-profile"
                : "/candidate/dashboard";

        if (user.UserType == UserType.Recruiter)
            return profileStatus == "incomplete"
                ? "/employer/complete-profile"
                : "/employer/dashboard";

        return "/";
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