// ============================================================
//  JobPortal.Services/Implement/Candidate/ApplicationStatusService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Applications;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class ApplicationStatusService : IApplicationStatusService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApplicationStatusService> _logger;

    public ApplicationStatusService(AppDbContext context, ILogger<ApplicationStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApplicationStatusResponseDto> GetApplicationStatusAsync(
        Guid candidateId, ApplicationStatusFilterDto filter)
    {
        try
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Include(a => a.RecruiterNotes)
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.StatusUpdatedAt)
                .ToListAsync();

            var allCards = apps.Select(a =>
            {
                var job = a.JobPosting;
                var isConfidential = job.CompanyVisibility == CompanyVisibility.ShowName;
                var latestNote = a.RecruiterNotes?
                    .OrderByDescending(n => n.UpdatedAt)
                    .FirstOrDefault();

                return new ApplicationStatusCardDto
                {
                    ApplicationId = a.ApplicationId,
                    JobId = job.JobId,
                    CompanyName = isConfidential
    ? null
    : (
        job.IsClientHiring &&
        job.ShowClientName &&
        !string.IsNullOrWhiteSpace(job.ClientName)
            ? job.ClientName
            : job.EmployerProfile?.CompanyDisplayName
      ),
                    CompanyLogoUrl = isConfidential ? null : job.EmployerProfile?.CompanyLogoUrl,
                    IsConfidentialCompany = isConfidential,
                    City = job.OnshoreCity,
                    State = job.OnshoreState,
                    JobTitle = job.JobTitle,
                    TradeCategory = job.TradeCategory,
                    EmploymentType = GetEmploymentType(job),
                    Tags = job.PublishingTags,
                    ApplicationStatus = a.ApplicationStatus.ToString(),
                    StageLabel = GetStageLabel(a.ApplicationStatus.ToString()),
                    StatusNote = GetStatusNote(a.ApplicationStatus.ToString(), job.JobTitle),
                    AppliedAt = a.AppliedAt,
                    AppliedAtDisplay = $"Applied: {a.AppliedAt:dd MMM yyyy}",
                    StatusUpdatedAt = a.StatusUpdatedAt,
                    StatusUpdatedAtDisplay = $"Updated: {a.StatusUpdatedAt:dd MMM yyyy}",
                    WithdrawalAllowed = a.WithdrawalAllowed &&
                                       a.ApplicationStatus.ToString() != "Hired" &&
                                       a.ApplicationStatus.ToString() != "Rejected",
                    RecruiterNote = latestNote == null ? null : new RecruiterNoteDto
                    {
                        RecruiterNoteId = latestNote.RecruiterNoteId,
                        NoteText = latestNote.NoteText,
                        UpdatedAt = latestNote.UpdatedAt,
                        UpdatedAtDisplay = $"Updated {latestNote.UpdatedAt:dd MMM yyyy}",
                        IsAcknowledged = latestNote.IsAcknowledged,
                        AcknowledgedAt = latestNote.AcknowledgedAt
                    }
                };
            }).ToList();

            var filtered = string.IsNullOrWhiteSpace(filter.Status) ||
                           filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? allCards
                : allCards.Where(c => NormalizeStatus(c.ApplicationStatus)
                    .Equals(NormalizeStatus(filter.Status), StringComparison.OrdinalIgnoreCase))
                  .ToList();

            var stats = new ApplicationSummaryStatsDto
            {
                TotalApplications = allCards.Count,
                ActivePipeline = allCards.Count(c =>
                    c.ApplicationStatus is "Applied" or "In Review" or "Shortlisted"),
                Interviews = allCards.Count(c => c.ApplicationStatus == "Interview"),
                Closed = allCards.Count(c =>
                    c.ApplicationStatus is "Rejected" or "Hired")
            };

            var counts = new ApplicationFilterCountsDto
            {
                All = allCards.Count,
                Applied = allCards.Count(c => c.ApplicationStatus == "Applied"),
                InReview = allCards.Count(c => c.ApplicationStatus == "In Review"),
                Shortlisted = allCards.Count(c => c.ApplicationStatus == "Shortlisted"),
                Interview = allCards.Count(c => c.ApplicationStatus == "Interview"),
                Rejected = allCards.Count(c => c.ApplicationStatus == "Rejected")
            };

            int pendingCount = allCards.Count(c =>
                c.RecruiterNote != null && !c.RecruiterNote.IsAcknowledged);

            return new ApplicationStatusResponseDto
            {
                Success = true,
                Message = $"{filtered.Count} application(s) found.",
                Stats = stats,
                FilterCounts = counts,
                Applications = filtered,
                PendingAcknowledgmentCount = pendingCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetApplicationStatusAsync failed. CandidateId={CandidateId}", candidateId);
            return new ApplicationStatusResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching your application status."
            };
        }
    }

    public async Task<AcknowledgeNoteResponseDto> AcknowledgeRecruiterNoteAsync(
        Guid applicationId, Guid candidateId)
    {
        try
        {
            var application = await _context.JobApplications
                .Include(a => a.RecruiterNotes)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.CandidateId == candidateId);

            if (application == null)
                return new AcknowledgeNoteResponseDto
                {
                    Success = false,
                    Message = "Application not found or does not belong to this candidate."
                };

            var note = application.RecruiterNotes?
                .OrderByDescending(n => n.UpdatedAt)
                .FirstOrDefault();

            if (note == null)
                return new AcknowledgeNoteResponseDto
                {
                    Success = false,
                    Message = "No recruiter note found for this application."
                };

            if (note.IsAcknowledged)
                return new AcknowledgeNoteResponseDto
                {
                    Success = false,
                    Message = "This note has already been acknowledged.",
                    ApplicationId = applicationId,
                    RecruiterNoteId = note.RecruiterNoteId,
                    IsAcknowledged = true,
                    AcknowledgedAt = note.AcknowledgedAt
                };

            note.IsAcknowledged = true;
            note.AcknowledgedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AcknowledgeNoteResponseDto
            {
                Success = true,
                Message = "Recruiter note acknowledged successfully.",
                ApplicationId = applicationId,
                RecruiterNoteId = note.RecruiterNoteId,
                IsAcknowledged = true,
                AcknowledgedAt = note.AcknowledgedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcknowledgeRecruiterNoteAsync failed. ApplicationId={ApplicationId}", applicationId);
            return new AcknowledgeNoteResponseDto
            {
                Success = false,
                Message = "An error occurred while acknowledging the note."
            };
        }
    }

    private static string GetStageLabel(string status) => status switch
    {
        "Applied" => "Application Submitted",
        "In Review" => "Application Under Review",
        "Shortlisted" => "Profile Shortlisted",
        "Interview" => "Interview Scheduled",
        "Rejected" => "Not Selected",
        "Hired" => "Offer Extended",
        "Withdrawn" => "Application Withdrawn",
        _ => status
    };

    private static string GetStatusNote(string status, string jobTitle) => status switch
    {
        "Applied" => "Your application has been submitted. The recruiter will review your profile.",
        "In Review" => "Hiring team is reviewing your profile, shift handling experience, and dispatch track record.",
        "Shortlisted" => "Your profile matched the role requirements. Recruiter has shortlisted your application.",
        "Interview" => "Trade test and supervisor interview have been scheduled. Check your email for location and timing.",
        "Rejected" => $"Thank you for applying for {jobTitle}. The employer has moved forward with other candidates.",
        "Hired" => $"Congratulations! You have been selected for {jobTitle}. Check your email for next steps.",
        "Withdrawn" => "You have withdrawn this application.",
        _ => string.Empty
    };

    private static string GetEmploymentType(JobPortal.Domain.Entities.JobPosting job)
    {
        var tags = job.PublishingTags;
        var known = new HashSet<string> { "Permanent", "Contract", "Temporary", "Internship" };
        return tags.FirstOrDefault(t => known.Contains(t)) ?? "Full time";
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }

    private static string NormalizeStatus(string? status) =>
        (status ?? string.Empty).Replace(" ", "").ToLower();
}