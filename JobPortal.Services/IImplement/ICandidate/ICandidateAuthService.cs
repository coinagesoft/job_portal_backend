using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateAuthService
{
    Task<CandidateRegisterResponseDto> RegisterAsync(
        CandidateRegisterRequestDto request,
        string ipAddress);

    Task<SendOtpResponseDto> SendOtpAsync(
        CandidateSendOtpRequestDto request,
        string ipAddress);

    Task<AuthResponseDto> VerifyOtpAsync(
        CandidateVerifyOtpRequestDto request,
        string ipAddress);
}