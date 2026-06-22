using System.Text.Json;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.AI;
using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using JobPortal.Application.DTOs.Recruiter.CVSearch;
namespace JobPortal.Services.Implement.AI;

public class JobMatchingService : IJobMatchingService
{
    private readonly AppDbContext _db;

    public JobMatchingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CandidateMatchResultDto> CalculateMatchAsync(
       Guid candidateId,
       Guid jobId)
    {
        // ==========================================
        // Candidate
        // ==========================================

        var candidate =
            await _db.CandidateProfiles
                .Include(x => x.Skills)
                .FirstOrDefaultAsync(
                    x => x.CandidateId == candidateId);

        // ==========================================
        // Job
        // ==========================================

        var job =
            await _db.JobPostings
                .FirstOrDefaultAsync(
                    x => x.JobId == jobId);

        if (candidate == null || job == null)
        {
            return EmptyResult();
        }

        // ==========================================
        // AI Similarity
        // ==========================================

        var aiScore =
            await GetEmbeddingScore(
                candidateId,
                jobId);

        // ==========================================
        // Skill Match
        // ==========================================

        int skillScore = 0;

        var candidateSkills =
            candidate.Skills
                .Select(x => x.SkillName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.ToLower())
                .ToList();

        List<string> jobSkills = new();

        if (!string.IsNullOrWhiteSpace(job.KeySkills))
        {
            try
            {
                jobSkills =
                    JsonSerializer.Deserialize<List<string>>(
                        job.KeySkills) ?? new();
            }
            catch
            {
                jobSkills = new();
            }
        }

        if (jobSkills.Any())
        {
            int matchedSkills = 0;

            foreach (var jobSkill in jobSkills)
            {
                if (candidateSkills.Any(x =>
     x.Equals(jobSkill.ToLower()) ||
     x.Contains(jobSkill.ToLower())))
                {
                    matchedSkills++;
                }
            }

            skillScore =
                jobSkills.Count == 0
                    ? 0
                    : (int)Math.Round(
                        (double)matchedSkills /
                        jobSkills.Count * 100);
        }

        // ==========================================
        // Trade Match
        // ==========================================

        int tradeScore = 0;

        if (!string.IsNullOrWhiteSpace(job.TradeCategory)
            &&
            !string.IsNullOrWhiteSpace(candidate.PrimaryTrade))
        {
            var candidateTrade =
      candidate.PrimaryTrade.ToLower();

            var jobTrade =
                job.TradeCategory.ToLower();

            if (candidateTrade.Contains(jobTrade) ||
                jobTrade.Contains(candidateTrade))
            {
                tradeScore = 100;
            }
            else
            {
                tradeScore = 0;
            }
        }

        // ==========================================
        // Experience Match
        // ==========================================

        int experienceScore = 0;

        if (job.ExperienceRequiredYears > 0)
        {
            experienceScore =
                Math.Min(
                    100,
                    (int)(
                        (double)candidate.TotalExperienceYears
                        / job.ExperienceRequiredYears
                        * 100));
        }
        int locationScore = 0;

        if (!string.IsNullOrWhiteSpace(job.OnshoreCity) &&
            !string.IsNullOrWhiteSpace(candidate.CurrentCity))
        {
            if (job.OnshoreCity.Equals(
                candidate.CurrentCity,
                StringComparison.OrdinalIgnoreCase))
            {
                locationScore = 100;
            }
        }
        // ==========================================
        // Final Score
        // ==========================================

        var finalScore =
        (int)Math.Round(
            aiScore * 0.35 +
            skillScore * 0.30 +
            tradeScore * 0.20 +
            experienceScore * 0.10 +
            locationScore * 0.05);

        // ==========================================
        // Reason
        // ==========================================
        string reason;

        if (skillScore >= 70)
        {
            reason = "Strong skill match";
        }
        else if (tradeScore == 100)
        {
            reason = "Trade matches requirement";
        }
        else if (experienceScore >= 80)
        {
            reason = "Experience matches requirement";
        }
        else
        {
            reason = "Partial profile match";
        }

        return new CandidateMatchResultDto
        {
            MatchScore = finalScore,
            MatchReason = reason,
            AiSimilarityScore = aiScore,
            SkillScore = skillScore,
            TradeScore = tradeScore,
            ExperienceScore = experienceScore,
            LocationScore = locationScore
        };
    }

    private async Task<int> GetEmbeddingScore(
    Guid candidateId,
    Guid jobId)
    {
        var candidateEmbedding =
            await _db.CandidateEmbeddings
                .FirstOrDefaultAsync(
                    x => x.CandidateId == candidateId);

        var jobEmbedding =
            await _db.JobEmbeddings
                .FirstOrDefaultAsync(
                    x => x.JobId == jobId);

        if (candidateEmbedding == null ||
            jobEmbedding == null)
        {
            return 0;
        }

        var candidateVector =
            JsonSerializer.Deserialize<float[]>(
                candidateEmbedding.EmbeddingJson);

        var jobVector =
            JsonSerializer.Deserialize<float[]>(
                jobEmbedding.EmbeddingJson);

        if (candidateVector == null ||
            jobVector == null)
        {
            return 0;
        }

        var similarity =
            CosineSimilarity(
                candidateVector,
                jobVector);

        return (int)Math.Round(similarity * 100);
    }

    private CandidateMatchResultDto EmptyResult()
    {
        return new CandidateMatchResultDto
        {
            MatchScore = 0,
            MatchReason = "No data",
            AiSimilarityScore = 0,
            SkillScore = 0,
            TradeScore = 0,
            ExperienceScore = 0,
            LocationScore = 0
        };
    }
    private static double CosineSimilarity(
        float[] a,
        float[] b)
    {
        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0;

        return dot /
               (Math.Sqrt(magA) *
                Math.Sqrt(magB));
    }
}