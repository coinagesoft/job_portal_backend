using FirebaseAdmin.Auth;
using JobPortal.Application.DTOs.Admin.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace JobPortal.Services.Implement.Admin;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(
        AppDbContext context,
        JwtService jwtService,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;

    }
    public async Task<AdminSendOtpResponseDto> SendOtpAsync(
        AdminSendOtpRequestDto request,
        string ipAddress)
    {
        try
        {
            var email = request.Email.Trim().ToLower();

            // 1. Find User
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                return new AdminSendOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid email address."
                };
            }

            // 2. Find Admin
            var admin = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.UserId);

            if (admin == null)
            {
                return new AdminSendOtpResponseDto
                {
                    Success = false,
                    Message = "Admin account not found."
                };
            }

            if (!admin.IsActive)
            {
                return new AdminSendOtpResponseDto
                {
                    Success = false,
                    Message = "Admin account is inactive."
                };
            }

            if (user.AccountStatus != AccountStatus.Active)
            {
                return new AdminSendOtpResponseDto
                {
                    Success = false,
                    Message = "Account is not active."
                };
            }

            if (admin.LockedUntil.HasValue &&
                admin.LockedUntil > DateTime.UtcNow)
            {
                var minutes = (int)Math.Ceiling(
                    (admin.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);

                return new AdminSendOtpResponseDto
                {
                    Success = false,
                    Message = $"Account locked. Try again in {minutes} minute(s)."
                };
            }

            // 3. Cooldown Check (60 sec)
            var lastOtp = await _context.AdminEmailOtps
                .Where(x =>
                    x.AdminId == admin.AdminId &&
                    x.Purpose == "Login")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null)
            {
                var seconds =
                    (DateTime.UtcNow - lastOtp.CreatedAt).TotalSeconds;

                if (seconds < 60)
                {
                    return new AdminSendOtpResponseDto
                    {
                        Success = false,
                        Message = $"Please wait {60 - (int)seconds} seconds before requesting another OTP.",
                        ResendAfterSeconds = 60 - (int)seconds
                    };
                }
            }

            // 4. Expire previous OTPs
            var activeOtps = await _context.AdminEmailOtps
                .Where(x =>
                    x.AdminId == admin.AdminId &&
                    !x.IsVerified &&
                    x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var otp in activeOtps)
            {
                otp.ExpiresAt = DateTime.UtcNow;
            }

            // 5. Generate OTP
            var otpCode = GenerateOtp();

            var otpEntity = new AdminEmailOtp
            {
                OtpId = Guid.NewGuid(),
                AdminId = admin.AdminId,
                Email = user.Email,
                OtpCode = otpCode,
                Purpose = "Login",
                Attempts = 0,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            _context.AdminEmailOtps.Add(otpEntity);

            await _context.SaveChangesAsync();

            // 6. Send Email
            await _emailService.SendAdminOtpEmailAsync(
     user.Email,
     otpCode);

            // 7. Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = admin.AdminId,
                PerformedByName = admin.AdminIdentifier,
                PerformedByRole = admin.Role.RoleName,
                Module = "Authentication",
                Action = "Send OTP",
                Description = "Login OTP sent to registered email.",
                IpAddress = ipAddress,
                Success = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AdminSendOtpResponseDto
            {
                Success = true,
                Message = "OTP has been sent to your registered email.",
                ExpiresAt = otpEntity.ExpiresAt,
                ResendAfterSeconds = 60
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.ToString());

            return new AdminSendOtpResponseDto
            {
                Success = false,
                Message = ex.ToString()
            };
        }
    }

    public async Task<AdminResendOtpResponseDto> ResendOtpAsync(
    AdminResendOtpRequestDto request,
    string ipAddress)
    {
        try
        {
            var email = request.Email.Trim().ToLower();

            //-------------------------------------------------------
            // Find User
            //-------------------------------------------------------

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                return new AdminResendOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid email address."
                };
            }

            //-------------------------------------------------------
            // Find Admin
            //-------------------------------------------------------

            var admin = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (admin == null || !admin.IsActive)
            {
                return new AdminResendOtpResponseDto
                {
                    Success = false,
                    Message = "Admin account is inactive."
                };
            }

            //-------------------------------------------------------
            // Account Locked?
            //-------------------------------------------------------

            if (admin.LockedUntil.HasValue &&
                admin.LockedUntil > DateTime.UtcNow)
            {
                var minutes = (int)Math.Ceiling(
                    (admin.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);

                return new AdminResendOtpResponseDto
                {
                    Success = false,
                    Message = $"Account locked. Try again in {minutes} minute(s)."
                };
            }

            //-------------------------------------------------------
            // Cooldown Check
            //-------------------------------------------------------

            var latestOtp = await _context.AdminEmailOtps
                .Where(x =>
                    x.AdminId == admin.AdminId &&
                    x.Purpose == "Login")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestOtp != null)
            {
                var seconds =
                    (DateTime.UtcNow - latestOtp.CreatedAt).TotalSeconds;

                if (seconds < 60)
                {
                    return new AdminResendOtpResponseDto
                    {
                        Success = false,
                        Message = $"Please wait {60 - (int)seconds} seconds before requesting another OTP.",
                        ResendAfterSeconds = 60 - (int)seconds
                    };
                }
            }

            //-------------------------------------------------------
            // Expire Existing OTPs
            //-------------------------------------------------------

            var activeOtps = await _context.AdminEmailOtps
                .Where(x =>
                    x.AdminId == admin.AdminId &&
                    !x.IsVerified &&
                    x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var item in activeOtps)
            {
                item.ExpiresAt = DateTime.UtcNow;
            }

            //-------------------------------------------------------
            // Generate OTP
            //-------------------------------------------------------

            var otpCode = GenerateOtp();

            var otp = new AdminEmailOtp
            {
                OtpId = Guid.NewGuid(),
                AdminId = admin.AdminId,
                Email = user.Email,
                OtpCode = otpCode,
                Purpose = "Login",
                Attempts = 0,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            _context.AdminEmailOtps.Add(otp);

            await _context.SaveChangesAsync();

            //-------------------------------------------------------
            // Send Email
            //-------------------------------------------------------
            await _emailService.SendAdminOtpEmailAsync(
                user.Email,
                otpCode);

            //-------------------------------------------------------
            // Audit Log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = admin.AdminId,
                PerformedByName = admin.AdminIdentifier,
                PerformedByRole = admin.Role.RoleName,
                Module = "Authentication",
                Action = "Resend OTP",
                Description = "Login OTP resent to registered email.",
                IpAddress = ipAddress,
                Success = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AdminResendOtpResponseDto
            {
                Success = true,
                Message = "OTP has been resent successfully.",
                ExpiresAt = otp.ExpiresAt,
                ResendAfterSeconds = 60
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resending admin OTP.");

            return new AdminResendOtpResponseDto
            {
                Success = false,
                Message = "Unable to resend OTP. Please try again."
            };
        }
    }

    public async Task<AdminVerifyOtpResponseDto> VerifyOtpAsync(
    AdminVerifyOtpRequestDto request,
    string ipAddress,
    string userAgent)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var email = request.Email.Trim().ToLower();

            //-------------------------------------------------------
            // 1. Find User
            //-------------------------------------------------------

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.UserType == UserType.Admin);

            if (user == null)
            {
                return Fail("Invalid email or OTP.");
            }

            //-------------------------------------------------------
            // 2. Find Admin
            //-------------------------------------------------------

            var admin = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (admin == null)
                return Fail("Admin account not found.");

            if (!admin.IsActive)
                return Fail("Admin account is inactive.");

            //-------------------------------------------------------
            // 3. Locked?
            //-------------------------------------------------------

            if (admin.LockedUntil.HasValue &&
                admin.LockedUntil > DateTime.UtcNow)
            {
                return Fail("Account is temporarily locked.");
            }

            //-------------------------------------------------------
            // 4. Latest OTP
            //-------------------------------------------------------

            var otp = await _context.AdminEmailOtps
                .Where(x =>
                    x.AdminId == admin.AdminId &&
                    x.Purpose == "Login")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return Fail("OTP not found.");

            //-------------------------------------------------------
            // 5. Already Used?
            //-------------------------------------------------------

            if (otp.IsVerified)
                return Fail("OTP already used.");

            //-------------------------------------------------------
            // 6. Expired?
            //-------------------------------------------------------

            if (otp.ExpiresAt < DateTime.UtcNow)
                return Fail("OTP expired.");

            //-------------------------------------------------------
            // 7. Validate OTP
            //-------------------------------------------------------

            if (otp.OtpCode != request.Otp)
            {
                otp.Attempts++;

                admin.FailedAttempts++;

                if (admin.FailedAttempts >= 5)
                {
                    admin.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                }

                await _context.SaveChangesAsync();

                return Fail("Invalid OTP.");
            }

            //-------------------------------------------------------
            // 8. Success
            //-------------------------------------------------------

            otp.IsVerified = true;

            admin.FailedAttempts = 0;
            admin.LockedUntil = null;

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            //-------------------------------------------------------
            // 9. JWT
            //-------------------------------------------------------
            var jwtId = Guid.NewGuid().ToString();

            var accessToken = _jwtService.GenerateAdminToken(
                admin,
                jwtId);

            var refreshToken = _jwtService.GenerateRefreshToken();

            var accessTokenExpiry = _jwtService.GetExpiry();

            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

            //-------------------------------------------------------
            // 10. Session
            //-------------------------------------------------------

            var session = new AdminSession
            {
                SessionId = Guid.NewGuid(),

                AdminId = admin.AdminId,

                JwtId = jwtId,

                RefreshToken = refreshToken,

                RefreshTokenExpiresAt = refreshTokenExpiry,

                IpAddress = ipAddress,

                UserAgent = userAgent,

                LoginAt = DateTime.UtcNow,

                ExpiresAt = accessTokenExpiry,

                IsRevoked = false
            };

            _context.AdminSessions.Add(session);

            //-------------------------------------------------------
            // 11. Login Log
            //-------------------------------------------------------

            _context.AdminLoginLogs.Add(new AdminLoginLog
            {
                LoginLogId = Guid.NewGuid(),
                AdminId = admin.AdminId,
                Email = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                LoginAt = DateTime.UtcNow,
                IsSuccess = true
            });

            //-------------------------------------------------------
            // 12. Audit Log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = admin.AdminId,
                PerformedByName = admin.AdminIdentifier,
                PerformedByRole = admin.Role.RoleName,
                Module = "Authentication",
                Action = "Login",
                Description = "Admin logged in successfully.",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            //-------------------------------------------------------
            // 13. Response
            //-------------------------------------------------------

            return new AdminVerifyOtpResponseDto
            {
                Success = true,

                Message = "Login successful.",

                AccessToken = accessToken,

                RefreshToken = refreshToken,

                ExpiresAt = accessTokenExpiry,

                Admin = new AdminProfileDto
                {
                    AdminId = admin.AdminId,
                    UserId = user.UserId,
                    AdminIdentifier = admin.AdminIdentifier,
                    FullName = admin.FullName,
                    Email = user.Email,
                    AdminType = admin.AdminType,
                    RoleId = admin.RoleId,
                    RoleName = admin.Role.RoleName,
                    IsActive = admin.IsActive
                },

                Permissions = new List<PermissionDto>()
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "VerifyOtp failed.");

            return Fail("Unable to verify OTP.");
        }
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(
    RefreshTokenRequestDto request)
    {
        var session = await _context.AdminSessions
            .Include(x => x.AdminUser)
                .ThenInclude(x => x.Role)
            .Include(x => x.AdminUser)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.RefreshToken == request.RefreshToken);

        if (session == null)
        {
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Invalid refresh token."
            };
        }

        if (session.IsRevoked)
        {
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Session has expired."
            };
        }

        if (session.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Refresh token expired."
            };
        }

        var admin = session.AdminUser;

        // Generate a new JWT Id
        var newJwtId = Guid.NewGuid().ToString();

        // Generate a new access token
        var accessToken = _jwtService.GenerateAdminToken(
            admin,
            newJwtId);

        // Generate a new refresh token
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Rotate session
        session.JwtId = newJwtId;

        session.RefreshToken = newRefreshToken;

        session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

        session.ExpiresAt = _jwtService.GetExpiry();

        await _context.SaveChangesAsync();

        return new RefreshTokenResponseDto
        {
            Success = true,
            Message = "Token refreshed successfully.",

            AccessToken = accessToken,

            RefreshToken = newRefreshToken,

            ExpiresAt = session.ExpiresAt
        };
    }

    public async Task<LogoutResponseDto> LogoutAsync(Guid adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //------------------------------------------------------
            // Find Admin
            //------------------------------------------------------

            var admin = await _context.AdminUsers
                .Include(x => x.User)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == adminId);

            if (admin == null)
            {
                return new LogoutResponseDto
                {
                    Success = false,
                    Message = "Admin not found."
                };
            }

            //------------------------------------------------------
            // Find Active Session
            //------------------------------------------------------

            var session = await _context.AdminSessions
                .Where(x =>
                    x.AdminId == adminId &&
                    !x.IsRevoked)
                .OrderByDescending(x => x.LoginAt)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.IsRevoked = true;
                session.LogoutAt = DateTime.UtcNow;
            }

            //------------------------------------------------------
            // Update Login Log
            //------------------------------------------------------

            var loginLog = await _context.AdminLoginLogs
                .Where(x =>
                    x.AdminId == adminId &&
                    x.LogoutAt == null)
                .OrderByDescending(x => x.LoginAt)
                .FirstOrDefaultAsync();

            if (loginLog != null)
            {
                loginLog.LogoutAt = DateTime.UtcNow;
            }

            //------------------------------------------------------
            // Audit Log
            //------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = admin.AdminId,
                PerformedByName = admin.AdminIdentifier,
                PerformedByRole = admin.Role.RoleName,

                Module = "Authentication",
                Action = "Logout",

                Description = "Admin logged out successfully.",

                IpAddress = session?.IpAddress ?? "Unknown",
                UserAgent = session?.UserAgent,

                Success = true,

                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new LogoutResponseDto
            {
                Success = true,
                Message = "Logged out successfully."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "Logout failed for AdminId {AdminId}",
                adminId);

            return new LogoutResponseDto
            {
                Success = false,
                Message = "Unable to logout."
            };
        }
    }

    public async Task<CurrentAdminResponseDto> GetCurrentAdminAsync(Guid adminId)
    {
        try
        {
            var admin = await _context.AdminUsers
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.AdminId == adminId &&
                    x.IsActive);

            if (admin == null)
            {
                return new CurrentAdminResponseDto
                {
                    Success = false,
                    Message = "Admin not found."
                };
            }

            if (admin.User.AccountStatus != AccountStatus.Active)
            {
                return new CurrentAdminResponseDto
                {
                    Success = false,
                    Message = "Account is inactive."
                };
            }

            var permissions = new List<PermissionDto>();


            return new CurrentAdminResponseDto
            {
                Success = true,
                Message = "Admin profile fetched successfully.",

                Admin = new AdminProfileDto
                {
                    AdminId = admin.AdminId,
                    UserId = admin.UserId,
                    AdminIdentifier = admin.AdminIdentifier,
                    FullName = admin.FullName,

                    Email = admin.User.Email,

                    AdminType = admin.AdminType,

                    RoleId = admin.RoleId,
                    RoleName = admin.Role.RoleName,

                    IsActive = admin.IsActive
                },

                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching current admin profile.");

            return new CurrentAdminResponseDto
            {
                Success = false,
                Message = "Unable to fetch profile."
            };
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
    private static AdminVerifyOtpResponseDto Fail(string message)
    {
        return new AdminVerifyOtpResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    private static bool IsOtpExpired(DateTime expiresAt)
    {
        return expiresAt <= DateTime.UtcNow;
    }
}