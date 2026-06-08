// ============================================================
//  JobPortal.Services/IImplement/ICandidate/ICandidateJobService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Jobs;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateJobService
{
    /// <summary>
    /// Returns paginated, filtered, and sorted active job listings.
    /// Supports all search/filter params from <see cref="CandidateJobSearchRequestDto"/>.
    /// </summary>
    Task<CandidateJobListResponseDto> GetJobsAsync(CandidateJobSearchRequestDto request);

    /// <summary>
    /// Returns full details of a single active job by its ID.
    /// Includes company info, full description, eligibility, screening questions,
    /// and a list of similar jobs.
    /// </summary>
    Task<CandidateJobDetailResponseDto> GetJobDetailAsync(Guid jobId);

    /// <summary>
    /// Toggles the saved/bookmark state of a job for a candidate.
    /// Creates a <see cref="SavedJob"/> record if not present; removes it if present.
    /// </summary>
    Task<SaveJobResponseDto> ToggleSaveJobAsync(Guid jobId, Guid candidateId);

    /// <summary>
    /// Returns dynamic filter options (distinct values from active jobs) for populating
    /// sidebar dropdowns on the jobs-list page.
    /// </summary>
    Task<JobFilterOptionsResponseDto> GetFilterOptionsAsync();
}