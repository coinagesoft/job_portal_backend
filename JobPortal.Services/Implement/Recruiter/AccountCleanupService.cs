using JobPortal.Infrastructure.Persistence;
using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter;

public class AccountCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountCleanupService> _logger;

    public AccountCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<AccountCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Account Cleanup Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupExpiredAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expiredUsers = await context.Users
            .Where(x =>
                x.IsDeleted &&
                x.RecoveryExpiry.HasValue &&
                x.RecoveryExpiry <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (!expiredUsers.Any())
        {
            _logger.LogInformation("No expired deleted accounts found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired deleted accounts.", expiredUsers.Count);

        foreach (var user in expiredUsers)
        {
            try
            {
                var employer = await context.EmployerProfiles
                    .FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);

                if (employer != null)
                {
                    await PurgeEmployerDataAsync(context, employer, user, cancellationToken);
                }

                // Whatever is left of the user row itself (covers both the
                // employer-owner case above and plain candidate/other
                // accounts that never had an EmployerProfile).
                context.Users.Remove(user);

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Permanently deleted account. UserId: {UserId}",
                    user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed deleting UserId {UserId}",
                    user.UserId);
            }
        }

        _logger.LogInformation("Account cleanup completed successfully.");
    }

    /// <summary>
    /// Hard-deletes every row tied to an employer account — jobs and
    /// everything that cascades from them, credit/billing/payment
    /// history, verification docs, settings, sub-users (including their
    /// own login accounts), sessions, OTPs, notifications, consent logs
    /// and support tickets — before finally removing the EmployerProfile
    /// itself. The owner's own User row is removed by the caller.
    ///
    /// Ordering matters: several FKs (JobPosting.EmployerId,
    /// RecruiterNote.EmployerId, EmployerPlanPurchase.EmployerId,
    /// PaymentTransaction.OriginalTxnId) are Restrict, not Cascade, so
    /// children must be deleted before their parents.
    /// </summary>
    private static async Task PurgeEmployerDataAsync(
        AppDbContext context,
        EmployerProfile employer,
        User owner,
        CancellationToken cancellationToken)
    {
        var employerId = employer.EmployerId;

        // Sub-users have their own User/login rows — capture the ids now,
        // before the EmployerSubUser link rows are removed below.
        var subUserUserIds = await context.EmployerSubUsers
            .Where(x => x.EmployerId == employerId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var allUserIds = new List<Guid> { owner.UserId };
        allUserIds.AddRange(subUserUserIds);

        // ---- Jobs and everything that cascades from them at the DB
        // level (JobApplications via JobId, RecruiterNotes via
        // ApplicationId). Job-linked rows that are NOT DB-cascaded are
        // cleared explicitly first.
        var employerJobIds = await context.JobPostings
            .Where(x => x.EmployerId == employerId)
            .Select(x => x.JobId)
            .ToListAsync(cancellationToken);

        if (employerJobIds.Count > 0)
        {
            await context.JobEmbeddings
                .Where(x => employerJobIds.Contains(x.JobId))
                .ExecuteDeleteAsync(cancellationToken);

            await context.SavedJobs
                .Where(x => employerJobIds.Contains(x.JobId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await context.JobPostings
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Credits & billing
        await context.CreditUsageTransactions
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CreditAllocationHistory
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.SubUserCreditAllocation
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.EmployerCandidateAccesses
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateUnlocks
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateCvDownloads
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.SecurityDeposits
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // Invoices point at PaymentTransactions, so they go first.
        await context.Invoices
            .Where(x => allUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        // PaymentTransaction.OriginalTxnId is a Restrict self-reference —
        // refund/child transactions have to be removed before the
        // original transaction they point back to.
        var employerTxns = await context.PaymentTransactions
            .Where(x => x.EmployerId == employerId || allUserIds.Contains(x.UserId))
            .OrderByDescending(x => x.OriginalTxnId != null) // children (has an original) first
            .ToListAsync(cancellationToken);

        context.PaymentTransactions.RemoveRange(employerTxns);
        await context.SaveChangesAsync(cancellationToken);

        await context.EmployerPlanPurchase
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CreditWallets
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Verification / badges
        await context.EmployerBadges
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.EmployerVerificationDocuments
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Settings & preferences
        await context.EmployerNotificationSettings
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.EmployerPreferences
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.SavedSearches
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Sub-user link rows, then the sub-users' own login accounts
        await context.EmployerSubUsers
            .Where(x => x.EmployerId == employerId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Per-user data for the owner AND every sub-user
        await context.UserSessions
            .Where(x => allUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.OtpVerifications
            .Where(x => x.UserId.HasValue && allUserIds.Contains(x.UserId.Value))
            .ExecuteDeleteAsync(cancellationToken);

        await context.ConsentLogs
            .Where(x => allUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Notifications
            .Where(x => allUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        // SupportTicketReplies cascade automatically via TicketId.
        await context.SupportTickets
            .Where(x => allUserIds.Contains(x.RaisedBy))
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Employer profile, then sub-users' User rows
        context.EmployerProfiles.Remove(employer);

        if (subUserUserIds.Count > 0)
        {
            var subUserRows = await context.Users
                .Where(x => subUserUserIds.Contains(x.UserId))
                .ToListAsync(cancellationToken);

            context.Users.RemoveRange(subUserRows);
        }
    }
}