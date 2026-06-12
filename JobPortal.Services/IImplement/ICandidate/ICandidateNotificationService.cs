using JobPortal.Application.DTOs.Recruiter.Notification;

public interface ICandidateNotificationService
{
    Task<NotificationListResponseDto> GetNotificationsAsync(Guid candidateId, string filter);
    Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(Guid candidateId);
}