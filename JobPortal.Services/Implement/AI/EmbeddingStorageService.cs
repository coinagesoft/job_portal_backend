using JobPortal.Domain.Entities;
using JobPortal.Domain.Entities.AI;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.AI;
using JobPortal.Services.IImplement.AI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class EmbeddingStorageService : IEmbeddingStorageService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddingService;

    public EmbeddingStorageService(
        AppDbContext db,
        IEmbeddingService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task GenerateCandidateEmbeddingAsync(
        Guid candidateId)
    {
        var candidate =
            await _db.CandidateProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(
                x => x.CandidateId == candidateId);

        if (candidate == null)
            return;

        var text =
            ProfileTextBuilder.BuildCandidate(candidate);

        var embedding =
            await _embeddingService
                .GenerateEmbeddingAsync(text);

        var json =
            JsonSerializer.Serialize(embedding);

        var existing =
            await _db.CandidateEmbeddings
                .FindAsync(candidateId);

        if (existing == null)
        {
            _db.CandidateEmbeddings.Add(
                new CandidateEmbedding
                {
                    CandidateId = candidateId,
                    EmbeddingJson = json,
                    UpdatedAt = DateTime.UtcNow
                });
        }
        else
        {
            existing.EmbeddingJson = json;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task GenerateJobEmbeddingAsync(Guid jobId)
    {
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(x => x.JobId == jobId);

        if (job == null)
            throw new Exception("Job not found");
        var text = $@"
Title: {job.JobTitle}
Trade: {job.TradeCategory}
Description: {job.JobDescription}
Skills: {string.Join(", ", job.KeySkills ?? new List<string>())}
Experience: {job.ExperienceMinYears}-{job.ExperienceMaxYears} Years
Location: {job.OnshoreCity}
";

        var embedding =
            await _embeddingService.GenerateEmbeddingAsync(text);

        var entity = await _db.JobEmbeddings
            .FirstOrDefaultAsync(x => x.JobId == jobId);

        if (entity == null)
        {
            entity = new JobEmbedding
            {
                JobId = jobId
            };

            _db.JobEmbeddings.Add(entity);
        }

        entity.EmbeddingJson =
            JsonSerializer.Serialize(embedding);

        entity.UpdatedAt =
            DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}