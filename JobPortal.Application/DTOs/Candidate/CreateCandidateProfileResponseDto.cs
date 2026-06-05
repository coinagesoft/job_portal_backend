namespace JobPortal.Application.DTOs.Candidate.Profile;

public class CreateCandidateProfileResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid CandidateId { get; set; }

    public byte ProfileCompletionPct { get; set; }
}