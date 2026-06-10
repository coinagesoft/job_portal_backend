
// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateItiInfoService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateItiInfoService : ICandidateItiInfoService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateItiInfoService> _logger;

    public CandidateItiInfoService(
        AppDbContext context,
        ILogger<CandidateItiInfoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ItiInfoResponseDto> GetItiInfoAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return ItiFail("Candidate profile not found.");

            return ItiOk(profile, "ITI info retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetItiInfoAsync failed for {Id}", candidateId);
            return ItiFail("Internal server error.");
        }
    }

    public async Task<UpdateItiInfoResponseDto> UpdateItiInfoAsync(
        Guid candidateId,
        UpdateItiInfoRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return ItiUpdateFail("Candidate profile not found.");

            // Apply fields
            profile.PrimaryTrade = request.PrimaryTrade;
            profile.ItiCertified = request.ItiCertified;
            profile.ItiTrade = request.ItiCertified ? request.ItiTrade : null;
            profile.ItiMarks = request.ItiCertified ? request.ItiMarks : null;
            profile.ItiCollege = request.ItiCertified ? request.ItiCollege : null;
            profile.UpdatedAt = DateTime.UtcNow;

            // Recalculate profile completion percentage
            profile.ProfileCompletionPct = RecalculatePct(profile);

            await _context.SaveChangesAsync();

            return new UpdateItiInfoResponseDto
            {
                Success = true,
                Message = "ITI info updated successfully.",
                Data = MapItiData(profile)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateItiInfoAsync failed for {Id}", candidateId);
            return ItiUpdateFail("Internal server error.");
        }
    }

    // ── private helpers ────────────────────────────────────────

    /// <summary>
    /// Simple stepped completion score.  Adjust weights to match your product spec.
    /// </summary>
    private static byte RecalculatePct(dynamic p)
    {
        int score = 0;
        if (!string.IsNullOrEmpty(p.FullName)) score += 15;
        if (p.DateOfBirth.HasValue) score += 5;
        if (!string.IsNullOrEmpty(p.Gender)) score += 5;
        if (!string.IsNullOrEmpty(p.CurrentCity)) score += 5;
        if (!string.IsNullOrEmpty(p.ProfessionalSummary)) score += 10;
        if (!string.IsNullOrEmpty(p.PrimaryTrade)) score += 15;
        if (p.ItiCertified) score += 10;
        if (p.TotalExperienceYears > 0) score += 10;
        if (!string.IsNullOrEmpty(p.NoticePeriod)) score += 5;
        // Capped at 80 here; remaining 20 come from docs/edu/work via other services
        return (byte)Math.Min(score, 80);
    }

    private static ItiInfoData MapItiData(dynamic p) => new()
    {
        CandidateId = p.CandidateId,
        PrimaryTrade = p.PrimaryTrade ?? string.Empty,
        ItiCertified = p.ItiCertified,
        ItiTrade = p.ItiTrade,
        ItiMarks = p.ItiMarks,
        ItiCollege = p.ItiCollege,
        ProfileCompletionPct = p.ProfileCompletionPct
    };

    private static ItiInfoResponseDto ItiOk(dynamic p, string msg) => new()
    {
        Success = true,
        Message = msg,
        Data = MapItiData(p)
    };

    private static ItiInfoResponseDto ItiFail(string msg)
        => new() { Success = false, Message = msg };

    private static UpdateItiInfoResponseDto ItiUpdateFail(string msg)
        => new() { Success = false, Message = msg };
}
