using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateLocationService : ICandidateLocationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateLocationService> _logger;

    public CandidateLocationService(
        AppDbContext context,
        ILogger<CandidateLocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CandidateLocationResponseDto> GetLocationAsync(Guid candidateId)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile == null)
            return new CandidateLocationResponseDto
            {
                Success = false,
                Message = "Candidate profile not found."
            };

        return new CandidateLocationResponseDto
        {
            Success = true,
            Message = "Location retrieved.",
            Data = new CandidateLocationData
            {
                CandidateId = profile.CandidateId,
                Latitude = profile.CurrentLatitude,
                Longitude = profile.CurrentLongitude,
                PermissionGranted = profile.LocationPermissionGranted,
                LocationUpdatedAt = profile.LocationUpdatedAt
            }
        };
    }

    public async Task<CandidateLocationResponseDto> UpdateLocationAsync(
        Guid candidateId,
        UpdateCandidateLocationRequestDto request)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile == null)
            return new CandidateLocationResponseDto
            {
                Success = false,
                Message = "Candidate profile not found."
            };

        profile.CurrentLatitude = request.Latitude;
        profile.CurrentLongitude = request.Longitude;
        profile.LocationPermissionGranted = request.PermissionGranted;
        profile.LocationUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Candidate {CandidateId} location synced at {UpdatedAt}",
            candidateId, profile.LocationUpdatedAt);

        return await GetLocationAsync(candidateId);
    }
}