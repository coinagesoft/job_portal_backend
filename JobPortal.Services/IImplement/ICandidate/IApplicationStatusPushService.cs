using JobPortal.Domain.Enums.RecruiterEnums;

namespace JobPortal.Services.IImplement.ICandidate;

public interface IApplicationStatusPushService
{
    /// <summary>
    /// Call AFTER the new status has been saved. Sends a push (and stores an in-app
    /// notification) to the candidate that owns the application — and only them.
    /// Never throws: a push failure must not fail the recruiter's status update.
    /// </summary>
    Task NotifyStatusChangedAsync(
        Guid applicationId,
        ApplicationStatus previousStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a test push to every device registered by this candidate.</summary>
    Task<int> SendTestAsync(
        Guid candidateId,
        CancellationToken cancellationToken = default);
}