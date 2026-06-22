namespace JobPortal.Services.IImplement.AI;

public interface IEmbeddingStorageService
{
    Task GenerateCandidateEmbeddingAsync(Guid candidateId);

    Task GenerateJobEmbeddingAsync(Guid jobId);
}