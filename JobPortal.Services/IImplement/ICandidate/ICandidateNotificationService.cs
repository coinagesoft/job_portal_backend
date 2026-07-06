using JobPortal.Application.DTOs.Recruiter.Notification;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateNotificationService
{
    Task<NotificationListResponseDto> GetNotificationsAsync(
        Guid candidateId,
        string filter);

    Task<bool> MarkNotificationAsReadAsync(
        Guid notificationId);

    Task<bool> MarkAllNotificationsAsReadAsync(
        Guid candidateId);

    Task<NotificationSettingsResponseDto?> GetNotificationSettingsAsync(
        Guid candidateId);

    Task<bool> UpdateNotificationSettingsAsync(
        Guid candidateId,
        UpdateNotificationSettingsDto request);
}