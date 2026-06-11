using JobPortal.Application.DTOs.Recruiter.Settings;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterSettingsService
    {
        // Account Settings

        Task<GetAccountSettingsResponseDto?> GetAccountSettingsAsync(
            Guid employerId);

        Task<UpdateAccountSettingsResponseDto> UpdateAccountSettingsAsync(
            Guid employerId,
            UpdateAccountSettingsRequestDto request);


        // Notification Settings

        Task<GetNotificationSettingsResponseDto?> GetNotificationSettingsAsync(
            Guid employerId);

        Task<UpdateNotificationSettingsResponseDto> UpdateNotificationSettingsAsync(
            Guid employerId,
            UpdateNotificationSettingsRequestDto request);


        // Preference Settings

        Task<GetPreferenceSettingsResponseDto?> GetPreferenceSettingsAsync(
            Guid employerId);

        Task<UpdatePreferenceSettingsResponseDto> UpdatePreferenceSettingsAsync(
            Guid employerId,
            UpdatePreferenceSettingsRequestDto request);


        // User Sessions

        Task<GetUserSessionsResponseDto?> GetUserSessionsAsync(
            Guid employerId);

        Task<RevokeSessionResponseDto> RevokeSessionAsync(
            Guid sessionId);
    }
}