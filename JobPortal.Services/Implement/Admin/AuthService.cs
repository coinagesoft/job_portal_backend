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

        if (user.AccountStatus == AccountStatus.Deleted)
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
            // ── Verify Firebase ID token ──────────────────
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

            // ── Token age check (max 5 minutes old) ───────
            var issuedAt = decodedToken.IssuedAtTimeSeconds;
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowSeconds - issuedAt > 300)
                return Fail("OTP session expired. Please request a new OTP.");

            // ── Extract phone_number from claims ──────────
            if (!decodedToken.Claims.TryGetValue(
                    "phone_number", out var phoneObj)
                || phoneObj is not string fullPhone
                || string.IsNullOrWhiteSpace(fullPhone))
            {
                return Fail("Phone number not found in token.");
            }

            // ── Find admin by full E.164 number ───────────
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.MobileNumber == fullPhone &&
                    x.UserType == UserType.Admin);

            if (user == null)
                return Fail("Access denied.");

            // ── Account status re-check ───────────────────
            // (status could have changed between step1 and step2)
            switch (user.AccountStatus)
            {
                case AccountStatus.Suspended:
                    return Fail("Account suspended. Contact support.");
                case AccountStatus.Deleted:
                    return Fail("Access denied.");
                case AccountStatus.Pending:
                    return Fail("Account is pending activation.");
            }

            // ── AdminUser re-check ────────────────────────
            var adminUser = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.UserId == user.UserId);

            if (adminUser == null || !adminUser.IsActive)
                return Fail("Admin account is inactive.");

            // ── Lockout re-check ──────────────────────────
            if (adminUser.LockedUntil.HasValue &&
                adminUser.LockedUntil.Value > DateTime.UtcNow)
            {
                var mins = (int)Math.Ceiling(
                    (adminUser.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                return Fail($"Account locked. Try again in {mins} minute(s).");
            }

            // ── Update last login ─────────────────────────
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // ── Generate JWT ──────────────────────────────
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

            return Fail("An unexpected error occurred. Please try again.");
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
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.MobileNumber == request.MobileNumber
                    &&
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                return new FirebaseCustomTokenResponseDto
                {
                    Success = false,
                    Message = "Admin not found."
                };
            }

            // Firebase UID
            var uid = user.UserId.ToString();

            // Additional claims
            var claims =
                new Dictionary<string, object>
                {
                { "phone_number", user.MobileNumber },
                { "role", user.UserType.ToString() }
                };

            // Generate Firebase custom token
            var firebaseToken =
                await FirebaseAuth.DefaultInstance
                    .CreateCustomTokenAsync(
                        uid,
                        claims);

            return new FirebaseCustomTokenResponseDto
            {
                Success = true,
                Message = "Firebase custom token generated.",
                FirebaseToken = firebaseToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating Firebase custom token.");

            return new FirebaseCustomTokenResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════
    private static AuthResponseDto Fail(string message) =>
        new() { Success = false, Message = message };
}