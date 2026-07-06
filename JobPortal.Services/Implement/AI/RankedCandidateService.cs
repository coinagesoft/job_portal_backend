using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.AI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobPortal.Services.Implement.AI;

/// <summary>
/// Fetches all candidates, calculates their AI match score against a job,
/// and returns them ranked highest-first with a full score breakdown.
/// Addresses ChatGPT recommendation #3 (score breakdown) and #4 (ranked endpoint).
/// </summary>
public class RankedCandidateService : IRankedCandidateService
{
    private readonly AppDbContext _db;
    private readonly IJobMatchingService _matchingService;

    public RankedCandidateService(
        AppDbContext db,
        IJobMatchingService matchingService)
    {
        _db = db;
        _matchingService = matchingService;
    }

    // ══════════════════════════════════════════════════════════
    // Get Ranked Candidates for a Job
    // ══════════════════════════════════════════════════════════

    public async Task<RankedCandidateListDto> GetRankedCandidatesAsync(
    RankedCandidateRequestDto request)
    {
        // ── Load job ──────────────────────────────────────────
        var job = await _db.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.JobId == request.JobId);

        if (job == null)
        {
            return new RankedCandidateListDto
            {
                JobId = request.JobId,
                JobTitle = "Unknown",
                TotalCandidatesEvaluated = 0,
                Candidates = new()
            };
        }

        // ── Load candidates ──────────────────────────────────
        var candidates = await _db.CandidateProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Skills)
            .ToListAsync();

        // ── Pre-load job skills once ─────────────────────────
        var jobSkillsLower =
            job.KeySkills?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .ToList()
            ?? new List<string>();

        // ── Score each candidate ─────────────────────────────
        var scoredList = new List<RankedCandidateDto>();

        foreach (var candidate in candidates)
        {
            var matchResult = await _matchingService.CalculateMatchAsync(
                candidate.CandidateId,
                request.JobId);

            if (matchResult.MatchScore < request.MinScore)
                continue;

            // ── Candidate Skills ─────────────────────────────
            var candidateSkillsLower = candidate.Skills
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillName))
                .Select(x => x.SkillName.Trim().ToLower())
                .ToList();

            // Optional: actual matched skills
            var matchedSkills = jobSkillsLower
                .Where(js =>
                    candidateSkillsLower.Any(cs =>
                        cs == js || cs.Contains(js)))
                .Distinct()
                .ToList();

            var label = matchResult.MatchScore switch
            {
                >= 80 => "Excellent",
                >= 60 => "Good",
                >= 40 => "Fair",
                _ => "Low"
            };

            scoredList.Add(new RankedCandidateDto
            {
                CandidateId = candidate.CandidateId,
                FullName = candidate.FullName,
                PrimaryTrade = candidate.PrimaryTrade,
                TotalExperienceYears = candidate.TotalExperienceYears,
                CurrentCity = candidate.CurrentCity,
                Band = candidate.Band,
                ProfilePhotoUrl = candidate.ProfilePhotoUrl,

                ScoreBreakdown = new ScoreBreakdownDto
                {
                    OverallScore = matchResult.MatchScore,
                    AiSimilarityScore = matchResult.AiSimilarityScore,
                    SkillScore = matchResult.SkillScore,
                    TradeScore = matchResult.TradeScore,
                    ExperienceScore = matchResult.ExperienceScore,
                    LocationScore = matchResult.LocationScore,
                    MatchReason = matchResult.MatchReason,
                    ScoreLabel = label
                }
            });
        }

        // ── Sort & Rank ──────────────────────────────────────
        var ranked = scoredList
            .OrderByDescending(x => x.ScoreBreakdown.OverallScore)
            .Take(request.Limit)
            .Select((candidate, index) =>
            {
                candidate.Rank = index + 1;
                return candidate;
            })
            .ToList();

        return new RankedCandidateListDto
        {
            JobId = request.JobId,
            JobTitle = job.JobTitle,
            TotalCandidatesEvaluated = candidates.Count,
            Candidates = ranked
        };
    }

    // ══════════════════════════════════════════════════════════
    // Get Score for One Candidate vs One Job (profile page)
    // ══════════════════════════════════════════════════════════

    public async Task<CandidateProfileScoreResponseDto?> GetCandidateProfileScoreAsync(
      Guid candidateId,
      Guid jobId)
    {
        // ── Validate both exist ───────────────────────────────

        var candidate = await _db.CandidateProfiles
            .AsNoTracking()
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        var job = await _db.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.JobId == jobId);

        if (candidate == null || job == null)
            return null;

        // ── Score ─────────────────────────────────────────────

        var matchResult = await _matchingService.CalculateMatchAsync(
            candidateId,
            jobId);

        // ── Skill Details ─────────────────────────────────────

        var jobSkills =
            job.KeySkills?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            ?? new List<string>();

        var candidateSkillsLower =
            candidate.Skills
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillName))
                .Select(x => x.SkillName.Trim().ToLower())
                .ToList();

        var matched = jobSkills
            .Where(jobSkill =>
                candidateSkillsLower.Any(candidateSkill =>
                    candidateSkill.Equals(jobSkill.ToLower()) ||
                    candidateSkill.Contains(jobSkill.ToLower()) ||
                    jobSkill.ToLower().Contains(candidateSkill)))
            .Distinct()
            .ToList();

        var missing = jobSkills
            .Where(jobSkill =>
                !candidateSkillsLower.Any(candidateSkill =>
                    candidateSkill.Equals(jobSkill.ToLower()) ||
                    candidateSkill.Contains(jobSkill.ToLower()) ||
                    jobSkill.ToLower().Contains(candidateSkill)))
            .Distinct()
            .ToList();

        // ── Score Label ───────────────────────────────────────

        var label = matchResult.MatchScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Low"
        };

        // ── Response ──────────────────────────────────────────

        return new CandidateProfileScoreResponseDto
        {
            CandidateId = candidateId,
            JobId = jobId,
            JobTitle = job.JobTitle,

            MatchedSkills = matched,
            MissingSkills = missing,

            ScoreBreakdown = new ScoreBreakdownDto
            {
                OverallScore = matchResult.MatchScore,
                AiSimilarityScore = matchResult.AiSimilarityScore,
                SkillScore = matchResult.SkillScore,
                TradeScore = matchResult.TradeScore,
                ExperienceScore = matchResult.ExperienceScore,
                LocationScore = matchResult.LocationScore,
                MatchReason = matchResult.MatchReason,
                ScoreLabel = label
            }
        };
    }
}
