using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Missing;

// ════════════════════════════════════════════════════════════════
// 1. AVAILABILITY STATUS
//    GET  /api/candidate/profile/availability
//    PUT  /api/candidate/profile/availability
// ════════════════════════════════════════════════════════════════

public class UpdateAvailabilityRequestDto
{
    [Required]
    [RegularExpression(
        "^(Available|Open_To_Opportunities|Not_Looking)$",
        ErrorMessage = "Must be: Available, Open_To_Opportunities, or Not_Looking")]
    public string AvailabilityStatus { get; set; } = "Available";
}

public class AvailabilityResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AvailabilityData? Data { get; set; }
}

public class AvailabilityData
{
    public Guid CandidateId { get; set; }
    public string AvailabilityStatus { get; set; } = "Available";
    public DateTime AvailabilityUpdatedAt { get; set; }
}


// ════════════════════════════════════════════════════════════════
// 2. ITI / TRADE INFO
//    GET  /api/candidate/profile/iti-info
//    PUT  /api/candidate/profile/iti-info
// ════════════════════════════════════════════════════════════════

public class ItiInfoResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ItiInfoData? Data { get; set; }
}

public class ItiInfoData
{
    public Guid CandidateId { get; set; }
    public string PrimaryTrade { get; set; } = string.Empty;
    public bool ItiCertified { get; set; }
    public string? ItiTrade { get; set; }
    public string? ItiMarks { get; set; }
    public string? ItiCollege { get; set; }
    public byte ProfileCompletionPct { get; set; }
}

public class UpdateItiInfoRequestDto
{
    [Required, MaxLength(150)]
    public string PrimaryTrade { get; set; } = string.Empty;

    public bool ItiCertified { get; set; }

    [MaxLength(150)]
    public string? ItiTrade { get; set; }

    [MaxLength(20)]
    public string? ItiMarks { get; set; }

    [MaxLength(200)]
    public string? ItiCollege { get; set; }
}

public class UpdateItiInfoResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ItiInfoData? Data { get; set; }
}


// ════════════════════════════════════════════════════════════════
// 3. AUTH — LOGOUT
//    POST /api/candidate/auth/logout
// ════════════════════════════════════════════════════════════════

public class CandidateLogoutRequestDto
{
    public string? FcmToken { get; set; }
}

public class CandidateLogoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}


// ════════════════════════════════════════════════════════════════
// 4. SAVED JOBS — PAGINATED
//    GET /api/candidate/jobs/saved/paged
// ════════════════════════════════════════════════════════════════

public class PagedSavedJobRequestDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    /// <summary>Active | Expired | Applied — omit for all</summary>
    public string? Filter { get; set; }
}

public class PagedSavedJobListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPrevPage { get; set; }

    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public int AppliedCount { get; set; }

    public List<PagedSavedJobCardDto> SavedJobs { get; set; } = new();
}

public class PagedSavedJobCardDto
{
    public Guid SavedJobId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? TradeCategory { get; set; }
    public string? EmploymentType { get; set; }
    public string JobStatus { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public bool HasApplied { get; set; }
    public string? ApplicationStatus { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime SavedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}


// ════════════════════════════════════════════════════════════════
// 5. APPLICATION STATUS — PAGINATED
//    GET /api/candidate/applications/status/paged
// ════════════════════════════════════════════════════════════════

public class PagedApplicationStatusRequestDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    /// <summary>Applied | InReview | Shortlisted | Interview | Rejected — omit for All</summary>
    public string? Status { get; set; }
}

public class PagedApplicationStatusResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPrevPage { get; set; }

    public PagedApplicationFilterCountsDto FilterCounts { get; set; } = new();
    public List<PagedApplicationCardDto> Applications { get; set; } = new();
}

public class PagedApplicationFilterCountsDto
{
    public int All { get; set; }
    public int Applied { get; set; }
    public int InReview { get; set; }
    public int Shortlisted { get; set; }
    public int Interview { get; set; }
    public int Rejected { get; set; }
    public int Hired { get; set; }
    public int Withdrawn { get; set; }
}

public class PagedApplicationCardDto
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? TradeCategory { get; set; }
    public string? EmploymentType { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ApplicationStatus { get; set; } = string.Empty;
    public string StageLabel { get; set; } = string.Empty;
    public string? StatusNote { get; set; }
    public bool WithdrawalAllowed { get; set; }
    public string? RecruiterNote { get; set; }
    public bool NoteAcknowledged { get; set; }
    public Guid? NoteId { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime StatusUpdatedAt { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
}