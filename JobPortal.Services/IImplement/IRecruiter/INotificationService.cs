using JobPortal.Application.DTOs.Recruiter.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface INotificationService
    {
        Task<NotificationListResponseDto> GetNotificationsAsync(
            Guid employerId,
            string filter);

        Task<bool> MarkNotificationAsReadAsync(
            Guid notificationId);

        Task<bool> MarkAllNotificationsAsReadAsync(
            Guid employerId);

        Task<NotificationSettingsResponseDto?> GetNotificationSettingsAsync(
            Guid employerId);

        Task<bool> UpdateNotificationSettingsAsync(
            Guid employerId,
            UpdateNotificationSettingsDto request);
    }
}
