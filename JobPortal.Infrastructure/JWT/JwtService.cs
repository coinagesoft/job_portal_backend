using JobPortal.Domain.Entities;
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

        public JwtService(
            IConfiguration configuration)
        {
            _configuration = configuration;
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


        public string GenerateToken(
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

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(
                        _configuration["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
        public DateTime GetExpiry()
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