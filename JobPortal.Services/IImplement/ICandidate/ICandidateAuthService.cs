using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Application.DTOs.Recruiter.Auth;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateAuthService
{
    Task<CandidateRegisterResponseDto> RegisterAsync(
        CandidateRegisterRequestDto request,
        string ipAddress);

    Task<SendOtpResponseDto> SendRegistrationOtpAsync(
        CandidateSendOtpRequestDto request,
        string ipAddress);

    Task<AuthResponseDto> VerifyOtpAsync(
        CandidateVerifyOtpRequestDto request,
        string ipAddress);

    Task<CreateCandidateOrderResponseDto> CreatePayPalOrderAsync(
        CreateCandidateOrderRequestDto request);

    Task<CreateCandidateOrderResponseDto> CreateOrderAsync(
    CreateCandidateOrderRequestDto request);
}