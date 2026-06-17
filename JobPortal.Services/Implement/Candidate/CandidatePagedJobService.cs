
// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidatePagedJobService.cs
//
//  Adds pagination to SavedJobs and ApplicationStatus.
//  Register these methods directly on the existing services OR
//  keep as a thin wrapper service — either approach works.
// ============================================================

using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidatePagedJobService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidatePagedJobService> _logger;

    public CandidatePagedJobService(
        AppDbContext context,
        ILogger<CandidatePagedJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════
    // PAGINATED SAVED JOBS
    // ════════════════════════════════════════════════
    public async Task<PagedSavedJobListResponseDto> GetPagedSavedJobsAsync(
        Guid candidateId,
        PagedSavedJobRequestDto req)
    {
        try
        {
            var page = Math.Max(1, req.Page);
            var pageSize = Math.Clamp(req.PageSize, 1, 50);

            // Base query
            var baseQuery = _context.SavedJobs
                .Include(s => s.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Where(s => s.CandidateId == candidateId);

            // Counts for filter badges (computed before filtering)
            var allSaved = await baseQuery
                .Select(s => new
                {
                    s.JobPosting.JobStatus,
                    HasApplied = _context.JobApplications
                        .Any(a => a.CandidateId == candidateId && a.JobId == s.JobId)
                })
                .ToListAsync();

            int activeCount = allSaved.Count(x => x.JobStatus == JobStatus.Active);
            int expiredCount = allSaved.Count(x => x.JobStatus != JobStatus.Active);
            int appliedCount = allSaved.Count(x => x.HasApplied);

            // Optional status filter
            if (!string.IsNullOrEmpty(req.Filter))
            {
                baseQuery = req.Filter.ToLower() switch
                {
                    "active" => baseQuery.Where(s => s.JobPosting.JobStatus == JobStatus.Active),
                    "expired" => baseQuery.Where(s => s.JobPosting.JobStatus != JobStatus.Active),
                    "applied" => baseQuery.Where(s => _context.JobApplications
                        .Any(a => a.CandidateId == candidateId && a.JobId == s.JobId)),
                    _ => baseQuery
                };
            }

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await baseQuery
                .OrderByDescending(s => s.SavedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Fetch application statuses for displayed items
            var jobIds = items.Select(s => s.JobId).ToList();
            var appStatuses = await _context.JobApplications
                .Where(a => a.CandidateId == candidateId && jobIds.Contains(a.JobId))
                .Select(a => new { a.JobId, a.ApplicationStatus })
                .ToListAsync();
            var appDict = appStatuses.ToDictionary(x => x.JobId, x => x.ApplicationStatus);

            var cards = items.Select(s =>
            {
                var job = s.JobPosting;
                var isConfidential = job.CompanyVisibility == "Confidential_Client";
                appDict.TryGetValue(job.JobId, out var appStatus);

                return new PagedSavedJobCardDto
                {
                    SavedJobId = s.SavedJobId,
                    JobId = job.JobId,
                    JobTitle = job.JobTitle,
                    CompanyName = isConfidential ? null : job.EmployerProfile?.CompanyDisplayName,
                    CompanyLogoUrl = isConfidential ? null : job.EmployerProfile?.CompanyLogoUrl,
                    IsConfidentialCompany = isConfidential,
                    City = job.OnshoreCity,
                    State = job.OnshoreState,
                    TradeCategory = job.TradeCategory,
                    EmploymentType = job.LocationType,
                    JobStatus = job.JobStatus.ToString() ?? string.Empty,
                    IsExpired = job.JobStatus.ToString() != "Active",
                    HasApplied = appStatus != null,
                    ApplicationStatus = appStatus.ToString(),
                    ApplicationDeadline = job.ApplicationDeadline.ToDateTime(TimeOnly.MinValue),
                    SavedAt = s.SavedAt,
                    Tags = ParseJsonList(job.PublishingTags)
                };
            }).ToList();

            return new PagedSavedJobListResponseDto
            {
                Success = true,
                Message = "Saved jobs retrieved.",
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPrevPage = page > 1,
                ActiveCount = activeCount,
                ExpiredCount = expiredCount,
                AppliedCount = appliedCount,
                SavedJobs = cards
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPagedSavedJobsAsync error for {Id}", candidateId);
            return new PagedSavedJobListResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching saved jobs."
            };
        }
    }


    // ════════════════════════════════════════════════
    // PAGINATED APPLICATION STATUS
    // ════════════════════════════════════════════════
    public async Task<PagedApplicationStatusResponseDto> GetPagedApplicationStatusAsync(
        Guid candidateId,
        PagedApplicationStatusRequestDto req)
    {
        try
        {
            var page = Math.Max(1, req.Page);
            var pageSize = Math.Clamp(req.PageSize, 1, 50);

            // Base query with navigation data
            var baseQuery = _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Include(a => a.RecruiterNotes)
                .Where(a => a.CandidateId == candidateId);

            // Compute filter counts before applying status filter
            var allStatuses = await baseQuery
                .Select(a => a.ApplicationStatus.ToString())
                .ToListAsync();

            var filterCounts = new PagedApplicationFilterCountsDto
            {
                All = allStatuses.Count,
                Applied = allStatuses.Count(x => x.ToString() == "Applied"),
                InReview = allStatuses.Count(x => x.ToString() == "InReview"),
                Shortlisted = allStatuses.Count(x => x.ToString() == "Shortlisted"),
                Interview = allStatuses.Count(x => x.ToString() == "Interview"),
                Rejected = allStatuses.Count(x => x.ToString() == "Rejected"),
                Hired = allStatuses.Count(x => x.ToString() == "Hired"),
                Withdrawn = allStatuses.Count(x => x.ToString() == "Withdrawn")
            };

            // Apply optional status filter
            if (!string.IsNullOrEmpty(req.Status))
                baseQuery = baseQuery.Where(a => a.ApplicationStatus.ToString() == req.Status);

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var apps = await baseQuery
                .OrderByDescending(a => a.StatusUpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var cards = apps.Select(a =>
            {
                var job = a.JobPosting;
                var isConfidential = job.CompanyVisibility == "Confidential_Client";
                var latestNote = a.RecruiterNotes?
                    .OrderByDescending(n => n.UpdatedAt)
                    .FirstOrDefault();

                return new PagedApplicationCardDto
                {
                    ApplicationId = a.ApplicationId,
                    JobId = job.JobId,
                    CompanyName = isConfidential ? null : job.EmployerProfile?.CompanyDisplayName,
                    CompanyLogoUrl = isConfidential ? null : job.EmployerProfile?.CompanyLogoUrl,
                    IsConfidentialCompany = isConfidential,
                    City = job.OnshoreCity,
                    State = job.OnshoreState,
                    JobTitle = job.JobTitle,
                    TradeCategory = job.TradeCategory,
                    EmploymentType = job.LocationType,
                    Tags = ParseJsonList(job.PublishingTags),
                    ApplicationStatus = a.ApplicationStatus.ToString() ?? "Applied",
                    StageLabel = GetStageLabel(a.ApplicationStatus.ToString()),
                    StatusNote = GetStatusNote(a.ApplicationStatus.ToString(), job.JobTitle),
                    WithdrawalAllowed = IsWithdrawalAllowed(a.ApplicationStatus.ToString()),
                    RecruiterNote = latestNote?.NoteText,
                    NoteAcknowledged = latestNote?.IsAcknowledged ?? false,
                    NoteId = latestNote?.RecruiterNoteId,
                    AppliedAt = a.AppliedAt,
                    StatusUpdatedAt = a.StatusUpdatedAt,
                    ApplicationDeadline = job.ApplicationDeadline.ToDateTime(TimeOnly.MinValue)
                };
            }).ToList();

            return new PagedApplicationStatusResponseDto
            {
                Success = true,
                Message = "Application status retrieved.",
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPrevPage = page > 1,
                FilterCounts = filterCounts,
                Applications = cards
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPagedApplicationStatusAsync error for {Id}", candidateId);
            return new PagedApplicationStatusResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching application status."
            };
        }
    }


    // ── shared helpers ─────────────────────────────────────────

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch { return new(); }
    }

    private static string GetStageLabel(string? status) => status switch
    {
        "Applied" => "Application Submitted",
        "InReview" => "Under Review",
        "Shortlisted" => "Shortlisted",
        "Interview" => "Interview Scheduled",
        "Hired" => "Hired",
        "Rejected" => "Not Selected",
        "Withdrawn" => "Withdrawn",
        _ => "Application Submitted"
    };

    private static string GetStatusNote(string? status, string jobTitle) => status switch
    {
        "Applied" => $"Your application for {jobTitle} is received.",
        "InReview" => "The recruiter is reviewing your profile.",
        "Shortlisted" => "Great news — you've been shortlisted!",
        "Interview" => "Check your email for interview details.",
        "Hired" => "Congratulations! You've been selected.",
        "Rejected" => "This role has been filled. Keep applying!",
        "Withdrawn" => "You have withdrawn this application.",
        _ => string.Empty
    };

    private static bool IsWithdrawalAllowed(string? status)
        => status is "Applied" or "InReview";
}
