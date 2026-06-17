namespace JobPortal.Domain.Entities;

/// <summary>
/// Stores per-candidate notification toggle preferences.
/// Mirrors EmployerNotificationSetting but for the candidate side.
/// </summary>
public class CandidateNotificationSetting
{
    public Guid NotifPrefId { get; set; }
    public Guid CandidateId { get; set; }

    // ── Notification toggles (matches the 5 on the UI) ──────────────
    public bool JobMatches { get; set; } = true;
    public bool ApplicationUpdates { get; set; } = true;
    public bool RecruiterMessages { get; set; } = true;
    public bool DocumentReminders { get; set; } = true;
    public bool OffersAnnouncements { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
