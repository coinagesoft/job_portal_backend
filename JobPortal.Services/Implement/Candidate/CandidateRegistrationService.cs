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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Candidate
{
    public class CandidateRegistrationService:ICandidateRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CandidateRegistrationService> _logger;
        private readonly ITwilioOtpService _twilioOtpService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        private const int OtpExpiryMinutes = 10;
        private const int ResendCooldownSeconds = 60;
        private const int MaxOtpAttempts = 5;

        public CandidateRegistrationService(
            AppDbContext context,
            JwtService jwtService,
            IEmailService emailService,
            ITwilioOtpService twilioOtpService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<CandidateRegistrationService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _config = config;
            _twilioOtpService = twilioOtpService;
            _httpClientFactory = httpClientFactory;
        }

        // ════════════════════════════════════════════════
        // GOOGLE REGISTER
        // ════════════════════════════════════════════════
        public async Task<AuthResponseDto> GoogleRegisterAsync(
            CandidateGoogleRegisterRequestDto request,
            string ipAddress)
        {
            try
            {
                if (!request.TermsAccepted)
                    return AuthFail("Terms and Conditions must be accepted.");

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", request.AccessToken);

                var googleResponse = await httpClient.GetAsync(
                    "https://www.googleapis.com/oauth2/v3/userinfo");

                if (!googleResponse.IsSuccessStatusCode)
                    return AuthFail("Invalid Google session.");

                var googleJson = await googleResponse.Content.ReadAsStringAsync();
                using var googleDoc = JsonDocument.Parse(googleJson);

                _logger.LogInformation("Google Status: {Status}", googleResponse.StatusCode);
                _logger.LogInformation("Google Response: {Response}", googleJson);
                _logger.LogInformation(
                "Incoming Mobile: '{Mobile}', Country: '{Country}'",
                request.MobileNumber,
                request.CountryCode);
                var root = googleDoc.RootElement;

                var email = root.TryGetProperty("email", out var emailProp)
                    ? emailProp.GetString()?.ToLower()
                    : null;

                var name = root.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(email))
                    return AuthFail("Google account email not found.");

                // Email must be unique
                if (await _context.Users.AnyAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == email))
                {
                    return AuthFail("Email is already registered. Please sign in instead.");
                }

                // Mobile must be unique
                if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    var mobile = request.MobileNumber.Trim();

                    if (await _context.Users.AnyAsync(u =>
                        u.MobileNumber == mobile))
                    {
                        return AuthFail("Mobile number is already registered.");
                    }
                }

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim(),
                    CountryCode = string.IsNullOrWhiteSpace(request.CountryCode)? null : request.CountryCode.Trim(),
                    PasswordHash = "GOOGLE_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                var profile = new JobPortal.Domain.Entities.CandidateProfile
                {
                    CandidateId = Guid.NewGuid(),
                    UserId = user.UserId,
                    FullName = name ?? "New Candidate",
                    ProfileStatus = "Incomplete",
                    ProfileCompletionPct = 0,
                    AvailabilityStatus = "Available",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CandidateProfiles.Add(profile);

                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber);

                _logger.LogInformation(
                    "New candidate registered via Google - UserId:{Id} IP:{IP}",
                    user.UserId, ipAddress);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful.",
                    Token = token,
                    UserId = user.UserId,
                    UserType = user.UserType.ToString(),
                    UserName = profile.FullName,
                    ProfileStatus = "Incomplete"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());

                throw;   // TEMPORARY
            }
        }

        // ════════════════════════════════════════════════
        // LINKEDIN REGISTER
        // ════════════════════════════════════════════════
        public async Task<AuthResponseDto> LinkedInRegisterAsync(
            CandidateLinkedInRegisterRequestDto request,
            string ipAddress)
        {
            try
            {
                if (!request.TermsAccepted)
                    return AuthFail("Terms and Conditions must be accepted.");

                var httpClient = _httpClientFactory.CreateClient();

                // Step 1: Exchange code for access token
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

                    return AuthFail("LinkedIn authentication failed. Please try again.");
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                using var tokenDoc = JsonDocument.Parse(tokenJson);

                var accessToken = tokenDoc.RootElement
                    .GetProperty("access_token")
                    .GetString();

                if (string.IsNullOrWhiteSpace(accessToken))
                    return AuthFail("LinkedIn authentication failed.");

                // Step 2: Get LinkedIn user profile
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var profileResponse = await httpClient.GetAsync(
                    "https://api.linkedin.com/v2/userinfo");

                if (!profileResponse.IsSuccessStatusCode)
                    return AuthFail("Failed to get LinkedIn profile.");

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

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

                if (existingUser != null)
                    return AuthFail("Account already exists. Please sign in instead.");

                if (!string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    var existingMobile = await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.MobileNumber == request.MobileNumber &&
                            u.CountryCode == request.CountryCode);

                    if (existingMobile != null)
                        return AuthFail("Mobile number already registered.");
                }

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = request.MobileNumber ?? "",
                    CountryCode = request.CountryCode ?? "+91",
                    PasswordHash = "LINKEDIN_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                var profile = new JobPortal.Domain.Entities.CandidateProfile
                {
                    CandidateId = Guid.NewGuid(),
                    UserId = user.UserId,
                    FullName = name ?? "New Candidate",
                    ProfileStatus = "Incomplete",
                    ProfileCompletionPct = 0,
                    AvailabilityStatus = "Available",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CandidateProfiles.Add(profile);

                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(
                    user.UserId,
                    user.UserType.ToString(),
                    user.MobileNumber);

                _logger.LogInformation(
                    "New candidate registered via LinkedIn - UserId:{Id} IP:{IP}",
                    user.UserId, ipAddress);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful.",
                    Token = token,
                    UserId = user.UserId,
                    UserType = user.UserType.ToString(),
                    UserName = profile.FullName,
                    ProfileStatus = "incomplete"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkedIn register error. IP:{IP}", ipAddress);
                return AuthFail("An error occurred. Please try again.");
            }
        }


        // ── Private Helpers ───────────────────────────────────

        private static AuthResponseDto AuthFail(string message) =>
            new() { Success = false, Message = message };
    }
}