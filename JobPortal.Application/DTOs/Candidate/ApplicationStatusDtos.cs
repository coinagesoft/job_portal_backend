// ============================================================
//  JobPortal.Application/DTOs/Candidate/ApplicationStatusDtos.cs
//
//  DTOs for:
//    1. Application Status page  (enhanced My Applications)
//    2. Recruiter Note           (read + acknowledge)
// ============================================================

namespace JobPortal.Application.DTOs.Candidate.Applications;

public class ApplicationStatusResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ApplicationSummaryStatsDto Stats { get; set; } = new();
    public List<ApplicationStatusCardDto> Applications { get; set; } = new();
    public ApplicationFilterCountsDto FilterCounts { get; set; } = new();
    public int PendingAcknowledgmentCount { get; set; }
}

public class ApplicationSummaryStatsDto
{
    public int TotalApplications { get; set; }
    public int ActivePipeline { get; set; }
    public int Interviews { get; set; }
    public int Closed { get; set; }
}

public class ApplicationFilterCountsDto
{
    public int All { get; set; }
    public int Applied { get; set; }
    public int InReview { get; set; }
    public int Shortlisted { get; set; }
    public int Interview { get; set; }
    public int Rejected { get; set; }
}

public class ApplicationStatusCardDto
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string JobTitle { get; set; } = default!;
    public string TradeCategory { get; set; } = default!;
    public string EmploymentType { get; set; } = default!;
    public List<string> Tags { get; set; } = new();
    /// <summary>Applied | In Review | Shortlisted | Interview | Rejected | Hired | Withdrawn</summary>
    public string ApplicationStatus { get; set; } = default!;
    /// <summary>Human-readable stage label for the badge, e.g. "Interview Scheduled".</summary>
    public string StageLabel { get; set; } = default!;
    /// <summary>Short contextual message shown below job title.</summary>
    public string StatusNote { get; set; } = default!;
    public DateTime AppliedAt { get; set; }
    public string AppliedAtDisplay { get; set; } = default!;       // "Applied: 29 Mar 2026"
    public DateTime StatusUpdatedAt { get; set; }
    public string StatusUpdatedAtDisplay { get; set; } = default!; // "Updated: 10 Apr 2026"
    /// <summary>Null when no recruiter note exists for this application.</summary>
    public RecruiterNoteDto? RecruiterNote { get; set; }
    public bool WithdrawalAllowed { get; set; }
}

public class RecruiterNoteDto
{
    public Guid RecruiterNoteId { get; set; }
    public string NoteText { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedAtDisplay { get; set; } = default!; // "Updated 10 Apr 2026"
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public class AcknowledgeNoteResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid RecruiterNoteId { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public class ApplicationStatusFilterDto
{
    /// <summary>Applied | InReview | Shortlisted | Interview | Rejected — omit for All</summary>
    public string? Status { get; set; }
}