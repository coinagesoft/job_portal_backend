using JobPortal.Infrastructure.Persistence;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
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

                // AsNoTracking: the profile is removed with ExecuteDelete below.
                // A tracked CandidateProfile would still reference the User row
                // that is removed afterwards (FK is Restrict) and make
                // SaveChanges throw.
                var candidate = await context.CandidateProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);

                if (candidate != null)
                {
                    await PurgeCandidateDataAsync(context, candidate, user, cancellationToken);
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

    /// <summary>
    /// Hard-deletes everything tied to a self-deleted CANDIDATE account once
    /// its 30-day recovery window has passed: applications (and the
    /// recruiter notes that cascade from them), saved jobs, documents, CVs,
    /// education/work/skills, KYC/passport/ITI reviews, embeddings, device
    /// tokens, settings, sessions, OTPs, notifications, consent logs and
    /// support tickets — then the CandidateProfile. The caller removes the
    /// User row afterwards.
    ///
    /// CandidateProfile → User and PaymentTransaction → CandidateProfile are
    /// Restrict, and several candidate children (education, work history,
    /// skills, CVs, KYC, passport, ITI, applications) are plain required
    /// FKs, so every dependent is cleared explicitly, leaf-to-root, rather
    /// than relying on cascade behaviour.
    ///
    /// The candidate's membership invoices and payment transactions are
    /// removed too (both have a required FK to the User row, so they could
    /// not survive its deletion anyway). Refund transactions are deleted
    /// before the originals they point back to (OriginalTxnId is Restrict).
    /// </summary>
    internal static async Task PurgeCandidateDataAsync(
        AppDbContext context,
        CandidateProfile candidate,
        User owner,
        CancellationToken cancellationToken)
    {
        var candidateId = candidate.CandidateId;
        var userId = owner.UserId;

        // ---- Applications (RecruiterNotes cascade via ApplicationId)
        await context.JobApplications
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.SavedJobs
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Employer-side records that point at this candidate
        await context.CandidateUnlocks
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.EmployerCandidateAccesses
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateCvDownloads
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        // CreditUsageTransaction.CandidateId is nullable — keep the
        // employer's billing history, just drop the link to the person.
        await context.CreditUsageTransactions
            .Where(x => x.CandidateId == candidateId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.CandidateId, (Guid?)null),
                cancellationToken);

        // ---- Candidate profile data
        await context.CandidateDocuments
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateCvs
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateEducations
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateWorkHistories
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateSkills
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.KycVerifications
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.PassportVerifications
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.ItiCertificateReviews
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateEmbeddings
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Settings, devices, logout sessions
        await context.CandidateNotificationSettings
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidatePreferenceSettings
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateDeviceTokens
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CandidateLogoutSessions
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Membership billing (coupons, invoices, then transactions)
        // CouponRedemption -> PaymentTransaction is Restrict, so the
        // redemptions must be removed before the transactions they point at.
        await context.CouponRedemptions
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Invoices
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var candidateTxns = await context.PaymentTransactions
            .Where(x => x.CandidateId == candidateId || x.UserId == userId)
            .OrderByDescending(x => x.OriginalTxnId != null) // refunds/children first
            .ToListAsync(cancellationToken);

        var candidateTxnIds = candidateTxns.Select(x => x.TransactionId).ToList();

        if (candidateTxnIds.Count > 0)
        {
            await context.CouponRedemptions
                .Where(x => x.PaymentTransactionId != null &&
                            candidateTxnIds.Contains(x.PaymentTransactionId.Value))
                .ExecuteDeleteAsync(cancellationToken);
        }

        context.PaymentTransactions.RemoveRange(candidateTxns);
        await context.SaveChangesAsync(cancellationToken);

        // ---- Per-user data
        await context.UserSessions
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // OTP rows are matched by number/email as well as user id, so no
        // stale code or resend-cooldown survives to block a fresh
        // registration with the same mobile number or email.
        var ownerMobile = owner.MobileNumber;
        var ownerEmail = owner.Email?.ToLower();

        await context.OtpVerifications
            .Where(x =>
                x.UserId == userId ||
                (ownerMobile != null && ownerMobile != "" && x.MobileNumber == ownerMobile) ||
                (ownerEmail != null && ownerEmail != "" &&
                    (x.Email == ownerEmail || x.MobileNumber == ownerEmail)))
            .ExecuteDeleteAsync(cancellationToken);

        await context.ConsentLogs
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Notifications
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // SupportTicketReplies cascade automatically via TicketId.
        await context.SupportTickets
            .Where(x => x.RaisedBy == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // ---- Finally the profile itself
        await context.CandidateProfiles
            .Where(x => x.CandidateId == candidateId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Registration safety net. If a CANDIDATE row with this email / mobile is
    /// still flagged IsDeleted (a soft-deleted leftover from before deletion
    /// became permanent, or one awaiting the 30-day cleanup), it is purged
    /// right now, so re-registering with the same email or number never hits
    /// "already registered" or a "suspended" warning. Active accounts and
    /// accounts that are really suspended by an admin (not IsDeleted) are
    /// never touched.
    /// </summary>
    internal static async Task PurgeSoftDeletedCandidatesAsync(
        AppDbContext context,
        string? email,
        string? mobile,
        CancellationToken cancellationToken = default)
    {
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();
        mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();

        if (email == null && mobile == null)
            return;

        var stale = await context.Users
            .AsNoTracking()
            .Where(u =>
                u.IsDeleted &&
                u.UserType == UserType.Candidate &&
                ((email != null && u.Email != null && u.Email.ToLower() == email) ||
                 (mobile != null && u.MobileNumber == mobile)))
            .ToListAsync(cancellationToken);

        foreach (var user in stale)
        {
            await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

            var profile = await context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);

            if (profile != null)
                await PurgeCandidateDataAsync(context, profile, user, cancellationToken);

            await context.Users
                .Where(u => u.UserId == user.UserId)
                .ExecuteDeleteAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
    }
}