using JobPortal.Application.DTOs.Recruiter.Notification;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateNotificationService : ICandidateNotificationService
{
    private readonly AppDbContext _context;

    public CandidateNotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponseDto> GetNotificationsAsync(
        Guid candidateId,
        string filter)
    {
        var userId = await _context.CandidateProfiles
            .Where(x => x.CandidateId == candidateId)
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
        Guid candidateId)
    {
        var userId = await _context.CandidateProfiles
            .Where(x => x.CandidateId == candidateId)
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

    public async Task<NotificationSettingsResponseDto?> GetNotificationSettingsAsync(
        Guid candidateId)
    {
        var setting = await _context.CandidateNotificationSettings
            .FirstOrDefaultAsync(x =>
                x.CandidateId == candidateId);

        if (setting == null)
            return null;

        return new NotificationSettingsResponseDto
        {
            NewApplicantAlerts = setting.JobMatches,
            CreditBillingAlerts = setting.OffersAnnouncements,
            JobStatusUpdates = setting.ApplicationUpdates,
            SystemMessages = setting.RecruiterMessages
        };
    }

    public async Task<bool> UpdateNotificationSettingsAsync(
        Guid candidateId,
        UpdateNotificationSettingsDto request)
    {
        var setting = await _context.CandidateNotificationSettings
            .FirstOrDefaultAsync(x =>
                x.CandidateId == candidateId);

        if (setting == null)
            return false;

        setting.JobMatches = request.NewApplicantAlerts;
        setting.OffersAnnouncements = request.CreditBillingAlerts;
        setting.ApplicationUpdates = request.JobStatusUpdates;
        setting.RecruiterMessages = request.SystemMessages;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}