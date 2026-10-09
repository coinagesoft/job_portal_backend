using JobPortal.Application.DTOs.Candidate.Push;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateDeviceTokenService
{
    /// <summary>
    /// Idempotent upsert of the caller's FCM token. Safe to call on every login,
    /// app start and onTokenRefresh.
    /// </summary>
    Task<DeviceTokenResponseDto> RegisterAsync(
        Guid candidateId,
        RegisterDeviceTokenRequestDto request);

    /// <summary>Removes the given token for this candidate (logout).</summary>
    Task<DeviceTokenResponseDto> UnregisterAsync(
        Guid candidateId,
        UnregisterDeviceTokenRequestDto request);
}