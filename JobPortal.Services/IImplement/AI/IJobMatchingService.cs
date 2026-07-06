using JobPortal.Application.DTOs.Recruiter.CVSearch;

namespace JobPortal.Services.IImplement.AI;

public interface IJobMatchingService
{
    Task<CandidateMatchResultDto>
        CalculateMatchAsync(
            Guid candidateId,
            Guid jobId);
}