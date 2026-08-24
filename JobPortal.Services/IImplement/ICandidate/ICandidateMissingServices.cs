using JobPortal.Application.DTOs.Candidate.Missing;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateAvailabilityService
{
    Task<AvailabilityResponseDto> GetAvailabilityAsync(Guid candidateId);
    Task<AvailabilityResponseDto> UpdateAvailabilityAsync(Guid candidateId, UpdateAvailabilityRequestDto request);
}

public interface ICandidateItiInfoService
{
    Task<ItiInfoResponseDto> GetItiInfoAsync(Guid candidateId);
    Task<UpdateItiInfoResponseDto> UpdateItiInfoAsync(Guid candidateId, UpdateItiInfoRequestDto request);
}

public interface ICandidateLogoutService
{
    Task<CandidateLogoutResponseDto> LogoutAsync(
        Guid candidateId,
        CandidateLogoutRequestDto request,
        string? jwtJti,
        DateTime? jwtExpiresAt);
}

public interface ICandidateLocationService
{
    Task<CandidateLocationResponseDto> GetLocationAsync(Guid candidateId);
    Task<CandidateLocationResponseDto> UpdateLocationAsync(Guid candidateId, UpdateCandidateLocationRequestDto request);
}