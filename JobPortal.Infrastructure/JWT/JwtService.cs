using JobPortal.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public string GenerateToken(User user,AdminUser adminUser)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.UserType.ToString()
                ),

                new Claim(
                    ClaimTypes.MobilePhone,
                    user.MobileNumber
                )
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["Jwt:Key"]!
                    )
                );

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

            var token =
                new JwtSecurityToken(
                    issuer:
                        _configuration["Jwt:Issuer"],

                    audience:
                        _configuration["Jwt:Audience"],

                    claims:
                        claims,

                    expires:
                        DateTime.UtcNow.AddMinutes(
                            Convert.ToDouble(
                                _configuration[
                                    "Jwt:ExpiryMinutes"
                                ]
                            )
                        ),

                    signingCredentials:
                        credentials
                );

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
    }
}