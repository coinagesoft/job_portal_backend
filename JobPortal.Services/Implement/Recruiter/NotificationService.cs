using JobPortal.Application.DTOs.Recruiter.Notification;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationListResponseDto> GetNotificationsAsync(
            Guid employerId,
            string filter)
        {
            var userId = await _context.EmployerProfiles
                .Where(x => x.EmployerId == employerId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync();

            var query = _context.Notifications
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter)
                && filter.ToLower() == "unread")
            {
                query = query.Where(x => !x.IsRead);
            }

            var notifications = await query
                .OrderByDescending(x => x.SentAt)
                .ToListAsync();

            return new NotificationListResponseDto
            {
                TotalCount = notifications.Count,
                UnreadCount = notifications.Count(x => !x.IsRead),

                Notifications = notifications.Select(x =>
                    new NotificationItemDto
                    {
                        NotificationId = x.NotificationId,
                        NotificationType = x.NotificationType,
                        Title = x.Title,
                        Body = x.Body,
                        IsRead = x.IsRead,
                        SentAt = x.SentAt
                    }).ToList()
            };
        }

        public async Task<bool> MarkNotificationAsReadAsync(
            Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x =>
                    x.NotificationId == notificationId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(
            Guid employerId)
        {
            var userId = await _context.EmployerProfiles
                .Where(x => x.EmployerId == employerId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync();

            var notifications = await _context.Notifications
                .Where(x => x.UserId == userId &&
                            !x.IsRead)
                .ToListAsync();

            foreach (var item in notifications)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<NotificationSettingsResponseDto?>
            GetNotificationSettingsAsync(Guid employerId)
        {
            var setting = await _context.EmployerNotificationSettings
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId);

            if (setting == null)
                return null;

            return new NotificationSettingsResponseDto
            {
                NewApplicantAlerts = setting.PrefApplicantNotify,
                CreditBillingAlerts = setting.PrefCreditExpiryEmail,
                JobStatusUpdates = setting.PrefJobStatusUpdates,
                SystemMessages = setting.PrefSystemMessages
            };
        }

        public async Task<bool> UpdateNotificationSettingsAsync(
            Guid employerId,
            UpdateNotificationSettingsDto request)
        {
            var setting = await _context.EmployerNotificationSettings
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId);

            if (setting == null)
                return false;

            setting.PrefApplicantNotify =
                request.NewApplicantAlerts;

            setting.PrefCreditExpiryEmail =
                request.CreditBillingAlerts;

            setting.PrefJobStatusUpdates =
                request.JobStatusUpdates;

            setting.PrefSystemMessages =
                request.SystemMessages;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
