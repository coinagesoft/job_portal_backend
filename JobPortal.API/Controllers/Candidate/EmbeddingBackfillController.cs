using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/embeddings")]
public class EmbeddingBackfillController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingStorageService _embeddingStorage;

    public EmbeddingBackfillController(
        AppDbContext db,
        IEmbeddingStorageService embeddingStorage)
    {
        _db = db;
        _embeddingStorage = embeddingStorage;
    }

    [HttpPost("backfill-jobs")]
    public async Task<IActionResult> BackfillJobs()
    {
        var jobIds = await _db.JobPostings
            .Select(x => x.JobId)
            .ToListAsync();

        int success = 0, failed = 0;
        var errors = new List<string>();

        foreach (var jobId in jobIds)
        {
            try
            {
                await _embeddingStorage.GenerateJobEmbeddingAsync(jobId);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{jobId}: {ex.Message}");
            }
        }

        return Ok(new { total = jobIds.Count, success, failed, errors });
    }

    [HttpPost("backfill-candidates")]
    public async Task<IActionResult> BackfillCandidates()
    {
        var candidateIds = await _db.CandidateProfiles
            .Select(x => x.CandidateId)
            .ToListAsync();

        int success = 0, failed = 0;
        var errors = new List<string>();

        foreach (var candidateId in candidateIds)
        {
            try
            {
                await _embeddingStorage.GenerateCandidateEmbeddingAsync(candidateId);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{candidateId}: {ex.Message}");
            }
        }

        return Ok(new { total = candidateIds.Count, success, failed, errors });
    }
}