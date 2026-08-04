// ============================================================
//  JobPortal.Services/IImplement/ICandidate/ICandidateJobService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Candidate.Jobs;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateJobService
{


    //Task<CandidateCompanyDetailResponseDto?> GetCompanyDetailAsync(
    //    Guid employerId);
    // ── Job Listing & Search ──────────────────────────────────
    Task<CandidateJobDetailResponseDto> GetJobDetailAsync(Guid jobId);
    Task<JobFilterOptionsResponseDto> GetFilterOptionsAsync();

    // ── Save / Unsave ─────────────────────────────────────────
    Task<SaveJobResponseDto> ToggleSaveJobAsync(Guid jobId, Guid candidateId);

    /// <summary>Returns all saved jobs for a candidate with their application status.</summary>
    Task<SavedJobListResponseDto> GetSavedJobsAsync(Guid candidateId);

    // ── Apply Now ─────────────────────────────────────────────
    /// <summary>
    /// Submits a job application for the candidate.
    /// Validates: job is active, deadline not passed, not already applied.
    /// Stores screening answers as JSON on the application record.
    /// Increments JobPosting.AppliedCount.
    /// </summary>
    Task<ApplyJobResponseDto> ApplyJobAsync(Guid jobId, Guid candidateId, ApplyJobRequestDto request);

   Task<ApplyJobDetailsResponseDto> GetApplyJobDetailsAsync( Guid jobId,Guid candidateId);
    // ── My Applications ───────────────────────────────────────
    /// <summary>Returns all applications submitted by this candidate, newest first.</summary>
    Task<MyApplicationsResponseDto> GetMyApplicationsAsync(Guid candidateId);

    /// <summary>
    /// Withdraws a previously submitted application.
    /// Only allowed when WithdrawalAllowed = true and status is not Hired/Rejected.
    /// </summary>
    Task<WithdrawApplicationResponseDto> WithdrawApplicationAsync(Guid applicationId, Guid candidateId);

    Task<List<CandidateJobListItemDto>> GetSimilarJobsAsync(
    Guid jobId,
    Guid? candidateId = null);
}