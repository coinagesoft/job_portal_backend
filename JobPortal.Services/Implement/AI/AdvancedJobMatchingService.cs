using System.Text.Json;
using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Entities.AI;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.AI;
using JobPortal.Services.IImplement.AI;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.AI;

public class AdvancedJobMatchingService : IJobMatchingService
{
    private readonly AppDbContext _db;

    public AdvancedJobMatchingService(
        AppDbContext db)
    {
        _db = db;
    }

    public async Task<CandidateMatchResultDto> CalculateMatchAsync(
        Guid candidateId,
        Guid jobId)
    {
        var candidateEmbedding =
            await _db.CandidateEmbeddings
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId);

        var jobEmbedding =
            await _db.JobEmbeddings
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId);

        var candidate =
            await _db.CandidateProfiles
                .Include(x => x.Skills)
                .Include(x => x.Educations)
                .FirstAsync(x =>
                    x.CandidateId == candidateId);

        var job =
            await _db.JobPostings
                .FirstAsync(x =>
                    x.JobId == jobId);

        var aiScore =
            CalculateEmbeddingScore(
                candidateEmbedding,
                jobEmbedding);

        var skillScore =
            CalculateSkillScore(
                candidate,
                job);

        var tradeScore =
            CalculateTradeScore(
                candidate,
                job);

        var experienceScore =
            CalculateExperienceScore(
                candidate,
                job);

        var locationScore =
            CalculateLocationScore(
                candidate,
                job);

        var educationScore =
            CalculateEducationScore(
                candidate);

        var finalScore =
            (aiScore * 0.40) +
            (skillScore * 0.25) +
            (tradeScore * 0.15) +
            (experienceScore * 0.10) +
            (locationScore * 0.05) +
            (educationScore * 0.05);

        return new CandidateMatchResultDto
        {
            MatchScore = (byte)Math.Round(finalScore),

            MatchReason =
                MatchReasonBuilder.Build(
                    tradeScore,
                    skillScore,
                    experienceScore,
                    locationScore),

            AiSimilarityScore = (byte)aiScore,
            SkillScore = (byte)skillScore,
            TradeScore = (byte)tradeScore,
            ExperienceScore = (byte)experienceScore,
            LocationScore = (byte)locationScore
        };
    }

    private int CalculateEmbeddingScore(
        CandidateEmbedding? candidate,
        JobEmbedding? job)
    {
        if (candidate == null || job == null)
            return 0;

        var candidateVector =
            JsonSerializer.Deserialize<float[]>(
                candidate.EmbeddingJson);

        var jobVector =
            JsonSerializer.Deserialize<float[]>(
                job.EmbeddingJson);

        if (candidateVector == null ||
            jobVector == null)
            return 0;

        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (int i = 0; i < candidateVector.Length; i++)
        {
            dot += candidateVector[i] * jobVector[i];
            magA += candidateVector[i] * candidateVector[i];
            magB += jobVector[i] * jobVector[i];
        }

        var similarity =
            dot /
            (Math.Sqrt(magA) *
             Math.Sqrt(magB));

        return (int)Math.Round(similarity * 100);
    }

    private int CalculateSkillScore(
        CandidateProfile candidate,
        JobPosting job)
    {
        if (string.IsNullOrWhiteSpace(job.KeySkills))
            return 0;

        var jobSkills =
            job.KeySkills
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(x => x.Trim().ToLower())
               .ToList();

        var candidateSkills =
            candidate.Skills
                .Select(x => x.SkillName.ToLower())
                .ToList();

        var matched =
            jobSkills.Count(x =>
                candidateSkills.Contains(x));

        return (int)
            ((double)matched /
             Math.Max(jobSkills.Count, 1) * 100);
    }

    private int CalculateTradeScore(
        CandidateProfile candidate,
        JobPosting job)
    {
        if (string.IsNullOrWhiteSpace(candidate.PrimaryTrade))
            return 0;

        return candidate.PrimaryTrade.Equals(
            job.TradeCategory,
            StringComparison.OrdinalIgnoreCase)
            ? 100
            : 0;
    }

    private int CalculateExperienceScore(
        CandidateProfile candidate,
        JobPosting job)
    {
        if (job.ExperienceRequiredYears == 0)
            return 100;

        return Math.Min(
            100,
            candidate.TotalExperienceYears * 100 /
            job.ExperienceRequiredYears);
    }

    private int CalculateLocationScore(
        CandidateProfile candidate,
        JobPosting job)
    {
        if (string.IsNullOrWhiteSpace(job.OnshoreCity))
            return 50;

        return candidate.CurrentCity?.Equals(
            job.OnshoreCity,
            StringComparison.OrdinalIgnoreCase) == true
            ? 100
            : 0;
    }

    private int CalculateEducationScore(
        CandidateProfile candidate)
    {
        if (!candidate.Educations.Any())
            return 0;

        return 100;
    }
}