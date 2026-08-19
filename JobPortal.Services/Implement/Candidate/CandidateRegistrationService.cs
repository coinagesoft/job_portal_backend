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
    public class CandidateRegistrationService : ICandidateRegistrationService
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

        // Same trust model as CandidateAuthService.RegisterAsync: never
        // trust a client-supplied amount, always re-read the plan (and
        // its price) from the DB using only the PlanId, and require it
        // to still be an active Candidate plan.
        private async Task<MembershipPlan?> ResolveAndValidateCandidatePlanAsync(Guid planId)
        {
            return await _context.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PlanId == planId &&
                    p.PlanType == PlanType.Candidate &&
                    p.IsActive);
        }

        private async Task<bool> IsPaymentAlreadyUsedAsync(string razorpayPaymentId)
        {
            return await _context.PaymentTransactions
                .AnyAsync(t =>
                    t.RazorpayPaymentId == razorpayPaymentId &&
                    t.PaymentStatus == "Completed");
        }
        public async Task<AuthResponseDto> GoogleRegisterAsync(CandidateGoogleRegisterRequestDto request, string ipAddress)
        {
            try
            {
                if (!request.TermsAccepted)
                    return AuthFail("Terms and Conditions must be accepted.");
                if (string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                    string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
                    string.IsNullOrWhiteSpace(request.RazorpaySignature))
                {
                    return AuthFail("Payment verification failed.");
                }

                if (await IsPaymentAlreadyUsedAsync(request.RazorpayPaymentId))
                {
                    return AuthFail("This payment has already been used to complete a registration.");
                }

                var membershipPlan = await ResolveAndValidateCandidatePlanAsync(request.PlanId);
                if (membershipPlan == null)
                {
                    return AuthFail("Selected membership plan is no longer available. Please refresh and try again.");
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", request.AccessToken);
                var googleResponse = await httpClient.GetAsync(
                    "https://www.googleapis.com/oauth2/v3/userinfo");
                if (!googleResponse.IsSuccessStatusCode)
                    return AuthFail("Invalid Google session.");
                var googleJson = await googleResponse.Content.ReadAsStringAsync();
                using var googleDoc = JsonDocument.Parse(googleJson);
                var root = googleDoc.RootElement;
                var email = root.TryGetProperty("email", out var emailProp)
                    ? emailProp.GetString()?.ToLower() : null;
                if (string.IsNullOrWhiteSpace(email))
                    return AuthFail("Google account email not found.");
                if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
                    return AuthFail("Email is already registered. Please sign in instead.");
                if (!string.IsNullOrWhiteSpace(request.MobileNumber) &&
                    await _context.Users.AnyAsync(u => u.MobileNumber == request.MobileNumber.Trim()))
                {
                    return AuthFail("Mobile number is already registered.");
                }
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim(),
                    CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim(),
                    PasswordHash = "GOOGLE_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Paid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                var googleMembershipAmountPaise = (int)Math.Round(membershipPlan.Price * 100, MidpointRounding.AwayFromZero);

                var profile = new JobPortal.Domain.Entities.CandidateProfile
                {
                    CandidateId = Guid.NewGuid(),
                    UserId = user.UserId,
                    FullName = request.FullName,
                    ProfileStatus = "Incomplete",
                    ProfileCompletionPct = 0,
                    AvailabilityStatus = "Available",
                    IsMember = true,
                    MembershipPlanId = membershipPlan.PlanId,
                    MembershipPurchasedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CandidateProfiles.Add(profile);

                // Store the candidate's registration payment — amount is
                // sourced from the admin-configured MembershipPlan.
                var paymentTransaction = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),

                    UserId = user.UserId,

                    CandidateId = profile.CandidateId,

                    TransactionType = "CandidateRegistration",

                    PackType = membershipPlan.PlanName,

                    AmountPaise = googleMembershipAmountPaise,

                    GstAmountPaise = 0,

                    TotalAmountPaise = googleMembershipAmountPaise,

                    PaymentMethod = "Razorpay",

                    RazorpayOrderId = request.RazorpayOrderId,

                    RazorpayPaymentId = request.RazorpayPaymentId,

                    PaymentStatus = "Completed",

                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(paymentTransaction);

                // Wrapped in an explicit transaction so the advisory lock
                // inside GenerateInvoiceNumberAsync actually guards the
                // insert it protects — see CandidateAuthService for the
                // full explanation.
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var invoiceNumber = await GenerateInvoiceNumberAsync();

                _context.Invoices.Add(new JobPortal.Domain.Entities.Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    TransactionId = paymentTransaction.TransactionId,
                    UserId = user.UserId,
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    InvoiceAmount = googleMembershipAmountPaise / 100,
                    InvoiceGst = 0,
                    InvoiceTotal = googleMembershipAmountPaise / 100,
                    InvoiceS3Url = null,
                    CreatedAt = DateTime.UtcNow,
                    PaymentTransaction = paymentTransaction
                });

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                var (token, _) = await _jwtService.GenerateTokenAsync(user.UserId, user.UserType.ToString(), user.MobileNumber, candidateId: profile.CandidateId);
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
                _logger.LogError(ex, "Google register error. IP:{IP}", ipAddress);
                return AuthFail("An error occurred. Please try again.");
            }
        }
        public async Task<AuthResponseDto> LinkedInRegisterAsync(CandidateLinkedInRegisterRequestDto request, string ipAddress)
        {
            try
            {
                if (!request.TermsAccepted)
                    return AuthFail("Terms and Conditions must be accepted.");
                if (string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                    string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
                    string.IsNullOrWhiteSpace(request.RazorpaySignature))
                {
                    return AuthFail("Payment verification failed.");
                }

                if (await IsPaymentAlreadyUsedAsync(request.RazorpayPaymentId))
                {
                    return AuthFail("This payment has already been used to complete a registration.");
                }

                var membershipPlan = await ResolveAndValidateCandidatePlanAsync(request.PlanId);
                if (membershipPlan == null)
                {
                    return AuthFail("Selected membership plan is no longer available. Please refresh and try again.");
                }

                // No code exchange here — reuse the access token from the verify step
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", request.AccessToken);
                var profileResponse = await httpClient.GetAsync("https://api.linkedin.com/v2/userinfo");
                if (!profileResponse.IsSuccessStatusCode)
                    return AuthFail("LinkedIn session expired. Please try again.");
                var profileJson = await profileResponse.Content.ReadAsStringAsync();
                using var profileDoc = JsonDocument.Parse(profileJson);
                var email = profileDoc.RootElement.TryGetProperty("email", out var emailProp)
                    ? emailProp.GetString()?.ToLower() : null;
                if (string.IsNullOrWhiteSpace(email))
                    return AuthFail("LinkedIn email not found.");
                if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
                    return AuthFail("Email is already registered. Please sign in instead.");
                if (!string.IsNullOrWhiteSpace(request.MobileNumber) &&
                    await _context.Users.AnyAsync(u => u.MobileNumber == request.MobileNumber.Trim()))
                {
                    return AuthFail("Mobile number is already registered.");
                }
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = UserType.Candidate,
                    Email = email,
                    MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim(),
                    CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim(),
                    PasswordHash = "LINKEDIN_AUTH",
                    AccountStatus = AccountStatus.Active,
                    KycStatus = KycStatus.Pending,
                    PaymentStatus = PaymentStatus.Paid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                var linkedInMembershipAmountPaise = (int)Math.Round(membershipPlan.Price * 100, MidpointRounding.AwayFromZero);

                var profile = new JobPortal.Domain.Entities.CandidateProfile
                {
                    CandidateId = Guid.NewGuid(),
                    UserId = user.UserId,
                    FullName = request.FullName,
                    ProfileStatus = "Incomplete",
                    ProfileCompletionPct = 0,
                    AvailabilityStatus = "Available",
                    IsMember = true,
                    MembershipPlanId = membershipPlan.PlanId,
                    MembershipPurchasedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CandidateProfiles.Add(profile);

                // Store the candidate's registration payment — amount is
                // sourced from the admin-configured MembershipPlan.
                var paymentTransaction = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),

                    UserId = user.UserId,

                    CandidateId = profile.CandidateId,

                    TransactionType = "CandidateRegistration",

                    PackType = membershipPlan.PlanName,

                    AmountPaise = linkedInMembershipAmountPaise,

                    GstAmountPaise = 0,

                    TotalAmountPaise = linkedInMembershipAmountPaise,

                    PaymentMethod = "Razorpay",

                    RazorpayOrderId = request.RazorpayOrderId,

                    RazorpayPaymentId = request.RazorpayPaymentId,

                    PaymentStatus = "Completed",

                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(paymentTransaction);

                // Wrapped in an explicit transaction so the advisory lock
                // inside GenerateInvoiceNumberAsync actually guards the
                // insert it protects — see CandidateAuthService for the
                // full explanation.
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var invoiceNumber = await GenerateInvoiceNumberAsync();

                _context.Invoices.Add(new JobPortal.Domain.Entities.Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    TransactionId = paymentTransaction.TransactionId,
                    UserId = user.UserId,
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    InvoiceAmount = linkedInMembershipAmountPaise / 100,
                    InvoiceGst = 0,
                    InvoiceTotal = linkedInMembershipAmountPaise / 100,
                    InvoiceS3Url = null,
                    CreatedAt = DateTime.UtcNow,
                    PaymentTransaction = paymentTransaction
                });

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                var (token, _) = await _jwtService.GenerateTokenAsync(user.UserId, user.UserType.ToString(), user.MobileNumber, candidateId: profile.CandidateId);
                _logger.LogInformation("New candidate registered via LinkedIn - UserId:{Id} IP:{IP}", user.UserId, ipAddress);
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
        public async Task<SocialVerifyResponseDto> GoogleVerifyAsync(GoogleVerifyRequestDto request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.AccessToken);
            var googleResponse = await httpClient.GetAsync(
                "https://www.googleapis.com/oauth2/v3/userinfo");
            if (!googleResponse.IsSuccessStatusCode)
                return new SocialVerifyResponseDto { Success = false, Message = "Invalid Google session." };
            var googleJson = await googleResponse.Content.ReadAsStringAsync();
            using var googleDoc = JsonDocument.Parse(googleJson);
            var root = googleDoc.RootElement;
            var email = root.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()?.ToLower() : null;
            var name = root.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(email))
                return new SocialVerifyResponseDto { Success = false, Message = "Google account email not found." };
            var exists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email);
            if (exists)
                return new SocialVerifyResponseDto { Success = false, Message = "Email is already registered. Please sign in instead." };
            return new SocialVerifyResponseDto
            {
                Success = true,
                Email = email,
                FullName = name
            };
        }
        public async Task<SocialVerifyResponseDto> LinkedInVerifyAsync(LinkedInVerifyRequestDto request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var tokenResponse = await httpClient.PostAsync(
                "https://www.linkedin.com/oauth/v2/accessToken",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = request.LinkedInCode,
                    ["redirect_uri"] = request.RedirectUri,
                    ["client_id"] = _config["LinkedIn:ClientId"]!,
                    ["client_secret"] = _config["LinkedIn:ClientSecret"]!
                }));
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorBody = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "LinkedIn token exchange failed. Status:{Status} Body:{Body} RedirectUri:{RedirectUri}",
                    (int)tokenResponse.StatusCode, errorBody, request.RedirectUri);
                return new SocialVerifyResponseDto { Success = false, Message = "LinkedIn authentication failed." };
            }
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenJson);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
                return new SocialVerifyResponseDto { Success = false, Message = "LinkedIn authentication failed." };
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            var profileResponse = await httpClient.GetAsync("https://api.linkedin.com/v2/userinfo");
            if (!profileResponse.IsSuccessStatusCode)
                return new SocialVerifyResponseDto { Success = false, Message = "Failed to get LinkedIn profile." };
            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var profileDoc = JsonDocument.Parse(profileJson);
            var email = profileDoc.RootElement.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()?.ToLower() : null;
            var name = profileDoc.RootElement.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(email))
                return new SocialVerifyResponseDto { Success = false, Message = "LinkedIn email not found. Please ensure it's visible." };
            var exists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email);
            if (exists)
                return new SocialVerifyResponseDto { Success = false, Message = "Email is already registered. Please sign in instead." };
            return new SocialVerifyResponseDto
            {
                Success = true,
                Email = email,
                FullName = name,
                AccessToken = accessToken   // frontend holds onto this for the final register call
            };
        }
        // ── Private Helpers ───────────────────────────────────
        private static AuthResponseDto AuthFail(string message) =>
            new() { Success = false, Message = message };

        // Generates a sequential, per-month invoice number, e.g. INV-202607-0001.
        // Same implementation as RecruiterCreditPlanService.GenerateInvoiceNumberAsync
        // — see that file for the full explanation of the advisory-lock +
        // MAX-based (not COUNT-based) approach. MUST be called inside an open
        // db transaction so the advisory lock actually guards the invoice
        // insert that follows.
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
}