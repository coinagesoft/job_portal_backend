using JobPortal.Application.DTOs.Recruiter.CVSearch;

namespace JobPortal.Services.IImplement.AI;

public interface IRankedCandidateService
{
    /// <summary>
    /// Returns all candidates ranked by AI match score for a given job.
    /// </summary>
    Task<RankedCandidateListDto> GetRankedCandidatesAsync(
        RankedCandidateRequestDto request);

    /// <summary>
    /// Returns the detailed AI score breakdown for a single candidate
    /// against a specific job — used on the candidate profile page.
    /// </summary>
    Task<CandidateProfileScoreResponseDto?> GetCandidateProfileScoreAsync(
        Guid candidateId,
        Guid jobId);
}
