using FirebaseAdmin.Messaging;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FcmNotification = FirebaseAdmin.Messaging.Notification;

namespace JobPortal.Services.Implement.Candidate;

/// <summary>
/// Sends "your application status changed" pushes through the Firebase Admin SDK.
/// Relies on the default FirebaseApp created in Program.cs.
/// </summary>
public class ApplicationStatusPushService : IApplicationStatusPushService
{
    // Must match the Android notification channel created in the Flutter app.
    public const string AndroidChannelId = "application_updates";

    // Value of data["type"]; the Flutter app routes taps on it.
    public const string DataTypeApplicationStatus = "application_status";

    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(8);
    private const int MaxTokensPerCandidate = 500; // FCM multicast limit

    private readonly AppDbContext _context;
    private readonly ILogger<ApplicationStatusPushService> _logger;

    public ApplicationStatusPushService(
        AppDbContext context,
        ILogger<ApplicationStatusPushService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================================
    // Application status change
    // ==========================================================
    public async Task NotifyStatusChangedAsync(
        Guid applicationId,
        ApplicationStatus previousStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var app = await _context.JobApplications
                .AsNoTracking()
                .Where(a => a.ApplicationId == applicationId)
                .Select(a => new
                {
                    a.ApplicationId,
                    a.JobId,
                    a.CandidateId,
                    a.ApplicationStatus,
                    JobTitle = a.JobPosting.JobTitle,
                    CandidateName = a.CandidateProfile.FullName,
                    UserId = a.CandidateProfile.UserId,
                    CompanyName = a.EmployerProfile.CompanyDisplayName
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (app == null)
                return;

            // Nothing changed (recruiter re-saved the same stage) or the change was
            // made by the candidate themselves -> no push.
            if (app.ApplicationStatus == previousStatus ||
                app.ApplicationStatus == ApplicationStatus.Withdrawn ||
                app.ApplicationStatus == ApplicationStatus.Applied)
                return;

            // Candidate's own toggle (Settings > Notifications > Application updates).
            // No settings row yet == default (enabled).
            var applicationUpdatesEnabled = await _context.CandidateNotificationSettings
                .AsNoTracking()
                .Where(s => s.CandidateId == app.CandidateId)
                .Select(s => (bool?)s.ApplicationUpdates)
                .FirstOrDefaultAsync(cancellationToken) ?? true;

            if (!applicationUpdatesEnabled)
                return;

            var userActive = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId == app.UserId && !u.IsDeleted, cancellationToken);

            if (!userActive)
                return;

            var statusLabel = ToLabel(app.ApplicationStatus);
            var title = BuildTitle(app.ApplicationStatus);
            var body = $"{app.CandidateName}, your application for {app.JobTitle} is now {statusLabel}.";

            // In-app inbox entry (shown by GET api/candidate/notification/notifications/{id}).
            var inApp = new JobPortal.Domain.Entities.Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = app.UserId,
                NotificationType = "ApplicationStatus",
                Channel = "Push",
                Title = title,
                Body = body,
                ReferenceId = app.ApplicationId,
                ReferenceType = "JobApplication",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };
            _context.Notifications.Add(inApp);
            await _context.SaveChangesAsync(cancellationToken);

            var tokens = await GetTokensAsync(app.CandidateId, cancellationToken);
            if (tokens.Count == 0)
            {
                _logger.LogInformation(
                    "No device tokens for candidate {CandidateId}; push skipped (in-app notification stored).",
                    app.CandidateId);
                return;
            }

            var data = new Dictionary<string, string>
            {
                ["type"] = DataTypeApplicationStatus,
                ["notificationId"] = inApp.NotificationId.ToString(),
                ["applicationId"] = app.ApplicationId.ToString(),
                ["jobId"] = app.JobId.ToString(),
                ["status"] = app.ApplicationStatus.ToString(),
                ["statusLabel"] = statusLabel,
                ["jobTitle"] = app.JobTitle ?? string.Empty,
                ["candidateName"] = app.CandidateName ?? string.Empty,
                ["companyName"] = app.CompanyName ?? string.Empty
            };

            await SendAsync(
                tokens,
                title,
                body,
                data,
                collapseKey: $"application_{app.ApplicationId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Never break the recruiter's status update because of a push problem.
            _logger.LogError(ex,
                "Push notification failed for application {ApplicationId}", applicationId);
        }
    }

    // ==========================================================
    // Test push (gated by the controller)
    // ==========================================================
    public async Task<int> SendTestAsync(
        Guid candidateId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetTokensAsync(candidateId, cancellationToken);
        if (tokens.Count == 0)
            return 0;

        var data = new Dictionary<string, string>
        {
            ["type"] = "test",
            ["sentAt"] = DateTime.UtcNow.ToString("O")
        };

        return await SendAsync(
            tokens,
            "Test notification",
            "Push notifications are working on this device.",
            data,
            collapseKey: "test",
            cancellationToken);
    }

    // ==========================================================
    // Helpers
    // ==========================================================
    private async Task<List<string>> GetTokensAsync(Guid candidateId, CancellationToken ct)
    {
        return await _context.CandidateDeviceTokens
            .AsNoTracking()
            .Where(t => t.CandidateId == candidateId)
            .OrderByDescending(t => t.LastUsedAt)
            .Select(t => t.Token)
            .Take(MaxTokensPerCandidate)
            .ToListAsync(ct);
    }

    /// <returns>Number of devices FCM accepted the message for.</returns>
    private async Task<int> SendAsync(
        List<string> tokens,
        string title,
        string body,
        Dictionary<string, string> data,
        string collapseKey,
        CancellationToken cancellationToken)
    {
        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FcmNotification { Title = title, Body = body },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = FirebaseAdmin.Messaging.Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = AndroidChannelId,
                    // A newer update for the same application replaces the older one.
                    Tag = collapseKey
                }
            },
            Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string> { ["apns-priority"] = "10" },
                Aps = new Aps
                {
                    Sound = "default",
                    ThreadId = collapseKey
                }
            }
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SendTimeout);

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, timeout.Token);

        var dead = new List<string>();

        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess) continue;

            var code = (r.Exception as FirebaseMessagingException)?.MessagingErrorCode;

            _logger.LogWarning(
                "FCM send failed for token {Token}: {Code} {Error}",
                Mask(tokens[i]), code, r.Exception?.Message);

            // UNREGISTERED == app uninstalled / token expired -> prune it.
            // (Other errors, e.g. a bad service account, must NOT delete tokens.)
            if (code == MessagingErrorCode.Unregistered)
                dead.Add(tokens[i]);
        }

        if (dead.Count > 0)
        {
            var rows = await _context.CandidateDeviceTokens
                .Where(t => dead.Contains(t.Token))
                .ToListAsync(CancellationToken.None);

            _context.CandidateDeviceTokens.RemoveRange(rows);
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        return response.SuccessCount;
    }

    private static string BuildTitle(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Shortlisted => "You've been shortlisted",
        ApplicationStatus.Hired => "Congratulations! You're hired",
        ApplicationStatus.Rejected => "Application update",
        ApplicationStatus.Interview
            or ApplicationStatus.TableInterview
            or ApplicationStatus.LocationInterview => "Interview update",
        _ => "Application status updated"
    };

    private static string ToLabel(ApplicationStatus status) => status switch
    {
        ApplicationStatus.InReview => "In Review",
        ApplicationStatus.CvSelection => "CV Selection",
        ApplicationStatus.TableInterview => "Table Interview",
        ApplicationStatus.LocationInterview => "Location Interview",
        _ => status.ToString()
    };

    private static string Mask(string token)
        => token.Length <= 10 ? "***" : $"{token[..6]}…{token[^4..]}";
}