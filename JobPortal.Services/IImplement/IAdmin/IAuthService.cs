using JobPortal.Application.DTOs.Admin.Auth;
using JobPortal.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{

    public interface IAuthService
    {
        // Step 1 — Check admin exists before Firebase sends OTP
        Task<CheckAdminResponseDto> CheckAdminExistsAsync(
            CheckAdminRequestDto request, string ipAddress);

        // Step 2 — After Firebase OTP verified on frontend
        Task<AuthResponseDto> FirebaseLoginAsync(
            FirebaseLoginRequestDto request, string ipAddress);

        Task<AuthResponseDto> LogoutAsync(string adminId);

        Task<FirebaseCustomTokenResponseDto>
    GenerateFirebaseCustomTokenAsync(
        FirebaseCustomTokenRequestDto request);
    }
}