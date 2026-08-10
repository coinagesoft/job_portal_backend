using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Admin;

/// <summary>
/// Auto-resolves support tickets that have gone quiet.
///
/// Rule: if a ticket is not already "Resolved" and there has been no new
/// activity (no reply from the candidate/recruiter, and no reply from
/// admin) for 48 hours since the last message in the thread — or since
/// the ticket was raised, if nobody has replied at all — it is marked
/// "Resolved" automatically.
///
/// This is intentional: admins have no manual resolve/status endpoint in
/// this system (see AdminSupportTicketService). A ticket only closes
/// because (a) the ticket owner clicks "Resolve" on their own side, or
/// (b) this job closes it after 48 hours of silence.
///
/// Modeled directly on AccountCleanupService
/// (JobPortal.Services/Implement/Recruiter/AccountCleanupService.cs).
/// </summary>
public class SupportTicketAutoResolveService : BackgroundService
{
    private static readonly TimeSpan InactivityWindow = TimeSpan.FromHours(48);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SupportTicketAutoResolveService> _logger;

    public SupportTicketAutoResolveService(
        IServiceScopeFactory scopeFactory,
        ILogger<SupportTicketAutoResolveService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Support Ticket Auto-Resolve Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AutoResolveInactiveTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Support ticket auto-resolve run failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task AutoResolveInactiveTicketsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - InactivityWindow;

        // "Last activity" = UpdatedAt if the ticket has ever received a
        // reply from any side (see AdminSupportTicketService.AddReplyAsync
        // and the candidate/recruiter AddReplyAsync methods, all of which
        // stamp UpdatedAt on every new message); otherwise CreatedAt.
        var staleTickets = await context.SupportTickets
            .Where(t => t.Status != "Resolved")
            .Where(t => (t.UpdatedAt ?? t.CreatedAt) <= cutoff)
            .ToListAsync(cancellationToken);

        if (staleTickets.Count == 0)
        {
            _logger.LogInformation("No inactive support tickets to auto-resolve.");
            return;
        }

        _logger.LogInformation(
            "Auto-resolving {Count} support ticket(s) inactive for 48+ hours.",
            staleTickets.Count);

        var now = DateTime.UtcNow;

        foreach (var ticket in staleTickets)
        {
            ticket.Status = "Resolved";
            ticket.ResolvedAt = now;
            ticket.ResolutionNote ??= "Automatically resolved after 48 hours of no activity.";
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Support ticket auto-resolve run completed.");
    }
}