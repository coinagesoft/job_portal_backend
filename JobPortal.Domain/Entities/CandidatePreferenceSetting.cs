namespace JobPortal.Domain.Entities;

/// <summary>
/// Stores candidate account-level preferences shown on the Settings page:
/// language, timezone, resume visibility, communication preference.
/// </summary>
public class CandidatePreferenceSetting
{
    public Guid PrefId { get; set; }
    public Guid CandidateId { get; set; }

    // ── Profile Preferences (UI dropdowns) ──────────────────────────
    public string PreferredLanguage { get; set; } = "English";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public string ResumeVisibility { get; set; } = "AppliedJobsOnly";
    // AppliedJobsOnly | AllVerifiedRecruiters | Private
    public string CommunicationPreference { get; set; } = "EmailAndInApp";
    // EmailAndInApp | EmailOnly | InAppOnly | SmsAndEmail

    // ── Security / 2FA snapshot ──────────────────────────────────────
    public bool TwoFactorEnabled { get; set; } = false;
    public DateTime? LastPasswordUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
