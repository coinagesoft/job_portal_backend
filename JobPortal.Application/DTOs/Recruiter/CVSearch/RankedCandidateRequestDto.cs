namespace JobPortal.Application.DTOs.Recruiter.CVSearch;

public class RankedCandidateRequestDto
{
    public Guid JobId { get; set; }

    public int MinScore { get; set; } = 0;

    public int Limit { get; set; } = 20;
}