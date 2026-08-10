// ============================================================
//  JobPortal.Application/DTOs/Candidate/CandidateSettingsDtos.cs
// ============================================================

using JobPortal.Domain.Enums.RecruiterEnums;
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
    public int TotalCount { get; set; }     // always 5
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
// POST   /api/candidate/settings/support/tickets/{candidateId}
// GET    /api/candidate/settings/support/tickets/{candidateId}
// GET    /api/candidate/settings/support/thread/{ticketId}
// POST   /api/candidate/settings/support/tickets/{ticketId}/reply/{candidateId}
// PATCH  /api/candidate/settings/support/tickets/{ticketId}/resolve
// GET    /api/candidate/settings/support/{candidateId}/summary
// ═══════════════════════════════════════════════════════════════

public class CandidateCreateTicketRequestDto
{
    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public SupportTicketType Category { get; set; }
    // ProfileResume | JobApplication | PaymentBilling |
    // AccountAccess | TechnicalIssue | Other

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}

public class CandidateCreateTicketResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
}

public class CandidateTicketListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public List<CandidateTicketItemDto> Tickets { get; set; } = new();
}

public class CandidateTicketDetailResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidateTicketItemDto? Data { get; set; }
}

public class CandidateTicketItemDto
{
    public Guid TicketId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public SupportTicketType Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";   // Open | InProgress | Resolved | Closed
    public string Priority { get; set; } = "Normal";
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CandidateTicketThreadResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public SupportTicketType Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<CandidateTicketReplyDto> Replies { get; set; } = new();
}

public class CandidateTicketReplyDto
{
    public Guid ReplyId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CandidateAddReplyRequestDto

{
    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}

public class CandidateAddReplyResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CandidateTicketSummaryDto
{
    public int TotalTickets { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
}