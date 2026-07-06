namespace JobPortal.Domain.Entities.AI;

public class CandidateEmbedding
{
    public Guid CandidateId { get; set; }

    public string EmbeddingJson { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public CandidateProfile Candidate { get; set; } = null!;
}