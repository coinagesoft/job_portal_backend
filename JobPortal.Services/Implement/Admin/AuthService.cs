using FirebaseAdmin.Auth;
using JobPortal.Application.DTOs.Admin.Auth;
using JobPortal.Application.DTOs.Auth;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Admin;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;   
    private readonly IConfiguration _configuration;
    public AuthService(
        AppDbContext context,
        JwtService jwtService,
        ILogger<AuthService> logger,
        IConfiguration configuration)                 
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
    }

    // ════════════════════════════════════════════════════
    // STEP 1 — Check admin in DB before Firebase sends OTP
    // ════════════════════════════════════════════════════
    public async Task<CheckAdminResponseDto> CheckAdminExistsAsync(
        CheckAdminRequestDto request, string ipAddress)
    {
        // +91 + 9876543210 → +919876543210 (E.164)
        
        // ── Find user ────────────────────────────────────
        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.MobileNumber == request.MobileNumber &&
                x.UserType == UserType.Admin);

        if (user == null)
        {
            _logger.LogWarning(
                "Admin check failed — not found: {Phone} IP:{IP}",
                request.MobileNumber, ipAddress);

            return new CheckAdminResponseDto
            {
                Success = false,
                Message = "This number is not registered as an admin."
            };
        }

        // ── Account status checks ─────────────────────────
        if (user.AccountStatus == AccountStatus.Suspended)
            return new CheckAdminResponseDto
            {
                Success = false,
                Message = "Account suspended. Contact support."
            };

        if (user.AccountStatus == AccountStatus.Rejected)
            return new CheckAdminResponseDto
            {
                Success = false,
                Message = "This number is not registered as an admin." // generic
            };

        if (user.AccountStatus != AccountStatus.Active)
            return new CheckAdminResponseDto
            {
                Success = false,
                Message = "Account is not active."
            };

        // ── AdminUser checks ──────────────────────────────
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(a => a.UserId == user.UserId);

        if (adminUser == null || !adminUser.IsActive)
            return new CheckAdminResponseDto
            {
                Success = false,
                Message = "Admin account is inactive."
            };

        // ── Lockout check ─────────────────────────────────
        if (adminUser.LockedUntil.HasValue &&
            adminUser.LockedUntil.Value > DateTime.UtcNow)
        {
            var mins = (int)Math.Ceiling(
                (adminUser.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);

            return new CheckAdminResponseDto
            {
                Success = false,
                Message = $"Account locked. Try again in {mins} minute(s)."
            };
        }

        // ── All checks passed ─────────────────────────────
        _logger.LogInformation(
            "Admin check passed for IP:{IP}", ipAddress); // don't log phone

        return new CheckAdminResponseDto
        {
            Success = true,
            Message = "Admin verified. OTP will be sent.",
            E164Number = request.MobileNumber
        };
    }

    // ════════════════════════════════════════════════════
    // STEP 2 — Verify Firebase token and return JWT
    // ════════════════════════════════════════════════════
    public async Task<AuthResponseDto> FirebaseLoginAsync(
      FirebaseLoginRequestDto request, string ipAddress)
    {
        try
        {
            // ── Verify Firebase ID token ───────────────────────
            FirebaseToken decodedToken;
            try
            {
                decodedToken = await FirebaseAuth.DefaultInstance
                    .VerifyIdTokenAsync(request.FirebaseToken);
            }
            catch (FirebaseAuthException fex)
            {
                _logger.LogWarning(
                    "Firebase token invalid: {Reason} IP:{IP}",
                    fex.AuthErrorCode, ipAddress);

                return Fail("Invalid or expired OTP session. Please try again.");
            }

            // ── Token age check (max 5 minutes) ───────────────
            var issuedAt = decodedToken.IssuedAtTimeSeconds;
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowSeconds - issuedAt > 300)
                return Fail("OTP session expired. Please request a new OTP.");

            // ── Extract phone_number claim ─────────────────────
            // Firebase gives full E.164: "+919075309705"
            if (!decodedToken.Claims.TryGetValue(
                    "phone_number", out var phoneObj)
                || phoneObj is not string fullPhone
                || string.IsNullOrWhiteSpace(fullPhone))
            {
                _logger.LogWarning(
                    "phone_number claim missing. Claims: {Claims}",
                    string.Join(", ", decodedToken.Claims.Keys));

                return Fail("Phone number not found in token.");
            }

            // ── Split E.164 → countryCode + mobileNumber ──────
            // fullPhone    = "+919075309705"
            // countryCode  = "+91"           (from request)
            // mobileNumber = "9075309705"    (matches DB column)

            if (!fullPhone.StartsWith(request.CountryCode))
            {
                _logger.LogWarning(
                    "Token phone {Phone} does not start with {Code}",
                    fullPhone, request.CountryCode);

                return Fail("Phone number does not match country code.");
            }

            var mobileNumber = fullPhone[request.CountryCode.Length..]; // "9075309705"

            // ── DB lookup — match both columns separately ──────
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.MobileNumber == mobileNumber &&       // "9075309705"
                    x.CountryCode == request.CountryCode && // "+91"
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                _logger.LogWarning(
                    "No admin found — mobile: {Mobile} code: {Code} IP: {IP}",
                    mobileNumber, request.CountryCode, ipAddress);

                return Fail("Access denied.");
            }

            // ── Account status checks ──────────────────────────
            switch (user.AccountStatus)
            {
                case AccountStatus.Suspended:
                    return Fail("Account suspended. Contact support.");
                case AccountStatus.Rejected:
                    return Fail("Access denied.");
                case AccountStatus.Pending:
                    return Fail("Account is pending activation.");
            }

            // ── AdminUser check ────────────────────────────────
            var adminUser = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.UserId == user.UserId);

            if (adminUser == null || !adminUser.IsActive)
                return Fail("Admin account is inactive.");

            // ── Lockout check ──────────────────────────────────
            if (adminUser.LockedUntil.HasValue &&
                adminUser.LockedUntil.Value > DateTime.UtcNow)
            {
                var mins = (int)Math.Ceiling(
                    (adminUser.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                return Fail($"Account locked. Try again in {mins} minute(s).");
            }

            // ── Update last login ──────────────────────────────
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // ── Generate JWT ───────────────────────────────────
            var token = _jwtService.GenerateToken(user, adminUser);
            var expiry = _jwtService.GetExpiry();

            _logger.LogInformation(
                "Admin {Identifier} logged in. IP:{IP}",
                adminUser.AdminIdentifier, ipAddress);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                Role = adminUser.AdminRole,
                AdminIdentifier = adminUser.AdminIdentifier,
                ExpiresAt = expiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during Firebase login. IP:{IP}", ipAddress);

            // TEMP — shows real error, remove after fixing
            return Fail($"DEBUG: {ex.GetType().Name} — {ex.Message} — {ex.InnerException?.Message}");
        }
    }

    // ════════════════════════════════════════════════════
    // LOGOUT — revoke session
    // ════════════════════════════════════════════════════
    public async Task<AuthResponseDto> LogoutAsync(string adminId)
    {
        try
        {
            if (!Guid.TryParse(adminId, out var parsedAdminId))
                return Fail("Invalid session.");

            // Find and expire active session
            var session = await _context.AdminSessions
                .Where(s =>
                    s.AdminId == parsedAdminId &&
                    s.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                // Set expiry to now — token is dead immediately
                session.ExpiresAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Admin {AdminId} logged out.", adminId);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Logged out successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for admin {AdminId}", adminId);
            return Fail("An error occurred during logout.");
        }
    }

    public async Task<FirebaseCustomTokenResponseDto>
        GenerateFirebaseCustomTokenAsync(
            FirebaseCustomTokenRequestDto request)
    {
        try
        {
            // ── Find admin by mobileNumber + countryCode ───────
            // DB stores separately: mobile_number="9075309705"
            //                       country_code="+91"
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.MobileNumber == request.MobileNumber &&
                    x.CountryCode == request.CountryCode &&   // ✅ added
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                return new FirebaseCustomTokenResponseDto
                {
                    Success = false,
                    Message = "Admin not found."
                };
            }

            // ── Build full E.164 for phone_number claim ────────
            // Firebase login will receive this claim and split it
            // countryCode="+91" + mobile="9075309705" = "+919075309705"
            var fullPhone = $"{user.CountryCode}{user.MobileNumber}";

            // ── Firebase UID = userId ──────────────────────────
            var uid = user.UserId.ToString();

            // ── Claims — phone_number MUST be full E.164 ──────
            var claims = new Dictionary<string, object>
        {
            { "phone_number", fullPhone },              // ✅ "+919075309705"
            { "role", user.UserType.ToString() }
        };

            // ── Generate custom token ──────────────────────────
            var firebaseToken = await FirebaseAuth.DefaultInstance
                .CreateCustomTokenAsync(uid, claims);

            return new FirebaseCustomTokenResponseDto
            {
                Success = true,
                Message = "Firebase custom token generated.",
                FirebaseToken = firebaseToken,
                PhoneUsed = fullPhone                       // ✅ helpful for debugging
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating Firebase custom token for mobile: {Mobile}",
                request.MobileNumber);

            return new FirebaseCustomTokenResponseDto
            {
                Success = false,
                Message = "Failed to generate token. Please try again."  // ✅ no ex.Message
            };
        }
    }
    // ════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════
    private static AuthResponseDto Fail(string message) =>
        new() { Success = false, Message = message };
}