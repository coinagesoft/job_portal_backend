using JobPortal.Application.DTOs.Recruiter.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.ICandidate
{
    public interface ICandidateLoginService
    {
        Task<SendOtpResponseDto> SendOtpAsync(
       SendOtpRequestDto request, string ipAddress);

        Task<AuthResponseDto> VerifyOtpAsync(
            VerifyOtpRequestDto request, string ipAddress);

        Task<AuthResponseDto> GoogleLoginAsync(
            GoogleLoginRequestDto request, string ipAddress);

        Task<AuthResponseDto> LinkedInLoginAsync(
            LinkedInLoginRequestDto request, string ipAddress);
    }
}
