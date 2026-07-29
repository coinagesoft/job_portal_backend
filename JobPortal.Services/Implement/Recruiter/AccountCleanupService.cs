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
                    // =====================================================
                    // DELETE RELATED DATA HERE
                    // =====================================================
                    //
                    // Example:
                    //
                    // context.EmployerNotificationSettings.RemoveRange(
                    //     context.EmployerNotificationSettings
                    //         .Where(x => x.EmployerId == employer.EmployerId));
                    //
                    // context.CreditWallets.RemoveRange(
                    //     context.CreditWallets
                    //         .Where(x => x.EmployerId == employer.EmployerId));
                    //
                    // context.EmployerPlanPurchase.RemoveRange(
                    //     context.EmployerPlanPurchase
                    //         .Where(x => x.EmployerId == employer.EmployerId));
                    //
                    // Delete any other recruiter-related tables first.
                    //
                    // =====================================================

                    context.EmployerProfiles.Remove(employer);
                }

                context.Users.Remove(user);

                _logger.LogInformation(
                    "Deleted recruiter account. UserId: {UserId}",
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

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account cleanup completed successfully.");
    }
}