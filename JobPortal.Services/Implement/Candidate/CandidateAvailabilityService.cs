using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateAvailabilityService : ICandidateAvailabilityService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateAvailabilityService> _logger;

    private static readonly string[] AllowedStatuses =
    {
        "Available",
        "Open_To_Opportunities",
        "Not_Looking"
    };

    public CandidateAvailabilityService(
        AppDbContext context,
        ILogger<CandidateAvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AvailabilityResponseDto> GetAvailabilityAsync(Guid candidateId)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile == null)
            return new AvailabilityResponseDto
            {
                Success = false,
                Message = "Candidate profile not found."
            };

        return new AvailabilityResponseDto
        {
            Success = true,
            Message = "Availability retrieved.",
            Data = new AvailabilityData
            {
                CandidateId = profile.CandidateId,
                AvailabilityStatus = profile.AvailabilityStatus,
                AvailabilityUpdatedAt =
                    profile.AvailabilityUpdatedAt ?? profile.UpdatedAt
            }
        };
    }

    public async Task<AvailabilityResponseDto> UpdateAvailabilityAsync(
        Guid candidateId,
        UpdateAvailabilityRequestDto request)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile == null)
            return new AvailabilityResponseDto
            {
                Success = false,
                Message = "Candidate profile not found."
            };

        profile.AvailabilityStatus = request.AvailabilityStatus;
        profile.AvailabilityUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetAvailabilityAsync(candidateId);
    }
}