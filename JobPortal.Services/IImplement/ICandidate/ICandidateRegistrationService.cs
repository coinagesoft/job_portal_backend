using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.ICandidate
{
    public interface ICandidateRegistrationService
    {
        Task<AuthResponseDto> GoogleRegisterAsync(CandidateGoogleRegisterRequestDto request, string ipAddress);
        Task<AuthResponseDto> LinkedInRegisterAsync(CandidateLinkedInRegisterRequestDto request, string ipAddress);
    }
}
