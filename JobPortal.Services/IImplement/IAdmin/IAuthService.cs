using JobPortal.Application.DTOs.Admin.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{

    public interface IAuthService
    {
        /// <summary>
        /// Sends OTP to the registered admin email.
        /// </summary>
        Task<AdminSendOtpResponseDto> SendOtpAsync(
            AdminSendOtpRequestDto request,
            string ipAddress);

        /// <summary>
        /// Resends OTP after cooldown.
        /// </summary>
        Task<AdminResendOtpResponseDto> ResendOtpAsync(
            AdminResendOtpRequestDto request,
            string ipAddress);

        /// <summary>
        /// Verifies OTP and signs the admin in.
        /// </summary>
        Task<AdminVerifyOtpResponseDto> VerifyOtpAsync(
            AdminVerifyOtpRequestDto request,
            string ipAddress,
            string userAgent);

        Task<RefreshTokenResponseDto> RefreshTokenAsync(
    RefreshTokenRequestDto request);

        /// <summary>
        /// Logs the admin out and revokes the active session.
        /// </summary>
        Task<LogoutResponseDto> LogoutAsync(
            Guid adminId);

        /// <summary>
        /// Returns currently logged-in admin details.
        /// </summary>
        Task<CurrentAdminResponseDto> GetCurrentAdminAsync(
            Guid adminId);
    }
}