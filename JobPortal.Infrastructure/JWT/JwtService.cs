using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JobPortal.Infrastructure.JWT
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        // Mirrors the option list on the /admin/settings screen
        // (AdminSettingsService.AllowedSessionTimeouts). Kept here too so
        // token expiry stays in lock-step with whatever the admin picked.
        private static readonly Dictionary<string, double> SessionTimeoutMinutesByLabel = new()
        {
            ["15 Minutes"] = 15,
            ["30 Minutes"] = 30,
            ["1 Hour"] = 60,
            ["2 Hours"] = 120,
            // "Never" isn't literally infinite (JWTs need a hard expiry to
            // validate), so use a long-lived window that's effectively
            // "doesn't time out" for practical purposes.
            ["Never"] = 60 * 24 * 365 * 10
        };

        public JwtService(
            IConfiguration configuration,
            AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Resolves how long a Candidate/Employer session should last, based
        /// on the Session Timeout the admin configured on the
        /// "/admin/settings" screen (falls back to Jwt:ExpiryMinutes when no
        /// admin has ever saved a setting). This is what makes the admin's
        /// "Session Timeout" control actually apply to candidate/employer
        /// logins instead of only being persisted and never read.
        /// </summary>
        private async Task<double> GetCandidateEmployerSessionTimeoutMinutesAsync()
        {
            var fallbackMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"]);

            var configuredTimeout = await _context.AdminUserSettings
                .AsNoTracking()
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => x.SessionTimeout)
                .FirstOrDefaultAsync();

            if (configuredTimeout != null &&
                SessionTimeoutMinutesByLabel.TryGetValue(configuredTimeout, out var minutes))
            {
                return minutes;
            }

            return fallbackMinutes;
        }

        //public string GenerateToken(User user,AdminUser adminUser)
        //{
        //    var claims = new[]
        //    {
        //        new Claim(
        //            ClaimTypes.NameIdentifier,
        //            user.UserId.ToString()
        //        ),

        //        new Claim(
        //            ClaimTypes.Role,
        //            user.UserType.ToString()
        //        ),

        //        new Claim(
        //            ClaimTypes.MobilePhone,
        //            user.MobileNumber
        //        )
        //    };

        //    var key =
        //        new SymmetricSecurityKey(
        //            Encoding.UTF8.GetBytes(
        //                _configuration["Jwt:Key"]!
        //            )
        //        );

        //    var credentials =
        //        new SigningCredentials(
        //            key,
        //            SecurityAlgorithms.HmacSha256
        //        );

        //    var token =
        //        new JwtSecurityToken(
        //            issuer:
        //                _configuration["Jwt:Issuer"],

        //            audience:
        //                _configuration["Jwt:Audience"],

        //            claims:
        //                claims,

        //            expires:
        //                DateTime.UtcNow.AddMinutes(
        //                    Convert.ToDouble(
        //                        _configuration[
        //                            "Jwt:ExpiryMinutes"
        //                        ]
        //                    )
        //                ),

        //            signingCredentials:
        //                credentials
        //        );

        //    return new JwtSecurityTokenHandler()
        //        .WriteToken(token);
        //}


        public async Task<(string Token, DateTime Expiry)> GenerateTokenAsync(
     Guid userId,
     string role,
     string? mobileNumber = null,
     Guid? employerId = null,
     Guid? candidateId = null,
     bool isSubUser = false)
        {
            var claims = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            userId.ToString()),

        new Claim(
            ClaimTypes.Role,
            role)
    };

            if (!string.IsNullOrWhiteSpace(mobileNumber))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.MobilePhone,
                        mobileNumber));
            }

            // Employer Id
            if (employerId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "EmployerId",
                        employerId.Value.ToString()));
            }

            // Candidate Id
            if (candidateId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "CandidateId",
                        candidateId.Value.ToString()));
            }

            // Sub-user flag — lets the API resolve "is this a sub-user"
            // straight from the signed token instead of trusting a
            // client-supplied header for it.
            claims.Add(
                new Claim(
                    "IsSubUser",
                    isSubUser ? "true" : "false"));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var sessionTimeoutMinutes = await GetCandidateEmployerSessionTimeoutMinutesAsync();
            var expiry = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
        }

        /// <summary>
        /// Expiry timestamp a freshly-issued Candidate/Employer token would
        /// get right now — kept in sync with GenerateTokenAsync so callers
        /// that need to report "expires at" without generating a new token
        /// (e.g. refresh-token flows) see the same admin-configured value.
        /// </summary>
        public async Task<DateTime> GetExpiryAsync()
        {
            var sessionTimeoutMinutes = await GetCandidateEmployerSessionTimeoutMinutesAsync();
            return DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes);
        }

        /// <summary>
        /// Fixed-config expiry (Jwt:ExpiryMinutes) — used for the Admin's own
        /// panel session, which is intentionally NOT affected by the
        /// candidate/employer "Session Timeout" setting on /admin/settings.
        /// </summary>
        public DateTime GetAdminExpiry()
        {
            return DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(
                    _configuration[
                        "Jwt:ExpiryMinutes"
                    ]
                )
            );
        }

        public string GenerateAdminToken(
      AdminUser admin,
      string jwtId)
        {
            var claims = new List<Claim>
    {
        new Claim("AdminId", admin.AdminId.ToString()),

        new Claim("UserId", admin.UserId.ToString()),

        new Claim(ClaimTypes.NameIdentifier, admin.UserId.ToString()),

        new Claim(ClaimTypes.Role, "Admin"),

        new Claim("AdminType", admin.AdminType),

        new Claim("RoleId", admin.RoleId.ToString()),

        new Claim("RoleName", admin.Role.RoleName),

        new Claim("IsSubUser", "false"),

        new Claim(JwtRegisteredClaimNames.Jti, jwtId)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }
    }
}