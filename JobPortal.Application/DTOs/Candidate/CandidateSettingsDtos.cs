// ============================================================
//  JobPortal.Application/DTOs/Candidate/CandidateSettingsDtos.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Settings;


// ═══════════════════════════════════════════════════════════════
// SECTION 1 — PROFILE PREFERENCES  (Settings main page)
// GET  /api/candidate/settings/preferences
// PUT  /api/candidate/settings/preferences
// ═══════════════════════════════════════════════════════════════

public class CandidatePreferenceResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidatePreferenceData? Data { get; set; }
}

public class CandidatePreferenceData
{
    public Guid CandidateId { get; set; }
    public string PreferredLanguage { get; set; } = "English";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public string ResumeVisibility { get; set; } = "AppliedJobsOnly";
    public string CommunicationPreference { get; set; } = "EmailAndInApp";
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastPasswordUpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string PlanName { get; set; } = "Candidate";
}

public class UpdateCandidatePreferenceRequestDto
{
    [Required, MaxLength(50)]
    public string PreferredLanguage { get; set; } = "English";

    [Required, MaxLength(100)]
    public string TimeZone { get; set; } = "Asia/Kolkata";

    [Required]
    public string ResumeVisibility { get; set; } = "AppliedJobsOnly";
    // AppliedJobsOnly | AllVerifiedRecruiters | Private

    [Required]
    public string CommunicationPreference { get; set; } = "EmailAndInApp";
    // EmailAndInApp | EmailOnly | InAppOnly | SmsAndEmail
}

public class UpdateCandidatePreferenceResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidatePreferenceData? Data { get; set; }
}


// ═══════════════════════════════════════════════════════════════
// SECTION 2 — NOTIFICATION PREFERENCES
// GET  /api/candidate/settings/notifications
// PUT  /api/candidate/settings/notifications
// PUT  /api/candidate/settings/notifications/reset
// ═══════════════════════════════════════════════════════════════

public class CandidateNotificationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidateNotificationData? Data { get; set; }
}

public class CandidateNotificationData
{
    public Guid CandidateId { get; set; }
    public bool JobMatches { get; set; }
    public bool ApplicationUpdates { get; set; }
    public bool RecruiterMessages { get; set; }
    public bool DocumentReminders { get; set; }
    public bool OffersAnnouncements { get; set; }
    public int EnabledCount { get; set; }   // how many toggles are ON
    public int TotalCount { get; set; }   // always 5
}

public class UpdateCandidateNotificationRequestDto
{
    public bool JobMatches { get; set; } = true;
    public bool ApplicationUpdates { get; set; } = true;
    public bool RecruiterMessages { get; set; } = true;
    public bool DocumentReminders { get; set; } = true;
    public bool OffersAnnouncements { get; set; } = true;
}


// ═══════════════════════════════════════════════════════════════
// SECTION 3 — HELP & SUPPORT
// POST /api/candidate/settings/support/tickets
// GET  /api/candidate/settings/support/tickets
// GET  /api/candidate/settings/support/tickets/{ticketId}
// ═══════════════════════════════════════════════════════════════

public class CreateSupportTicketRequestDto
{
    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;
    // ProfileResume | JobApplication | PaymentBilling |
    // AccountAccess | TechnicalIssue | Other

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}

public class CreateSupportTicketResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SupportTicketItemDto? Data { get; set; }
}

public class SupportTicketListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public List<SupportTicketItemDto> Tickets { get; set; } = new();
}

public class SupportTicketDetailResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SupportTicketItemDto? Data { get; set; }
}

public class SupportTicketItemDto
{
    public Guid TicketId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";   // Open | InProgress | Resolved | Closed
    public string Priority { get; set; } = "Normal";
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
