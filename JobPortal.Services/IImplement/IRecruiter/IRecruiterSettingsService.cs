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


        // Account Email / Mobile Change (OTP-gated)

        Task<SettingsOtpResponseDto> RequestEmailChangeOtpAsync(
            Guid employerId,
            RequestEmailChangeOtpRequestDto request);

        Task<SettingsOtpResponseDto> VerifyEmailChangeOtpAsync(
            Guid employerId,
            VerifyEmailChangeOtpRequestDto request);

        Task<SettingsOtpResponseDto> RequestMobileChangeOtpAsync(
            Guid employerId,
            RequestMobileChangeOtpRequestDto request);

        Task<SettingsOtpResponseDto> VerifyMobileChangeOtpAsync(
            Guid employerId,
            VerifyMobileChangeOtpRequestDto request);


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


        // Danger Zone

        Task<DangerZoneActionResponseDto> DeactivateAccountAsync(
            Guid employerId);

        Task<DangerZoneActionResponseDto> DeleteAllJobsAsync(
            Guid employerId);

        Task<DangerZoneActionResponseDto> DeleteAccountAsync(
            Guid employerId);

        Task<DangerZoneActionResponseDto> ReactivateAccountAsync(
            Guid employerId);
    }
}