using JobPortal.Application.DTOs.Candidate.Push;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateDeviceTokenService : ICandidateDeviceTokenService
{
    private static readonly HashSet<string> AllowedPlatforms =
        new(StringComparer.OrdinalIgnoreCase) { "android", "ios", "web" };

    private readonly AppDbContext _context;
    private readonly ILogger<CandidateDeviceTokenService> _logger;

    public CandidateDeviceTokenService(
        AppDbContext context,
        ILogger<CandidateDeviceTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DeviceTokenResponseDto> RegisterAsync(
        Guid candidateId,
        RegisterDeviceTokenRequestDto request)
    {
        try
        {
            var token = request.FcmToken.Trim();

            var candidateExists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);

            if (!candidateExists)
                return Fail("Candidate profile not found.");

            var platform = !string.IsNullOrWhiteSpace(request.Platform)
                           && AllowedPlatforms.Contains(request.Platform.Trim())
                ? request.Platform.Trim().ToLowerInvariant()
                : "unknown";

            var deviceId = string.IsNullOrWhiteSpace(request.DeviceId)
                ? null
                : request.DeviceId.Trim();

            // Retry once: two near-simultaneous calls (login + onTokenRefresh) can
            // both try to INSERT the same token and trip the unique index.
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    await UpsertAsync(candidateId, token, platform, deviceId, request.AppVersion);
                    return new DeviceTokenResponseDto
                    {
                        Success = true,
                        Message = "Device token registered."
                    };
                }
                catch (DbUpdateException) when (attempt == 1)
                {
                    _context.ChangeTracker.Clear();
                }
            }

            return Fail("Could not register device token.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterAsync failed for candidate {CandidateId}", candidateId);
            return Fail("Internal server error.");
        }
    }

    private async Task UpsertAsync(
        Guid candidateId,
        string token,
        string platform,
        string? deviceId,
        string? appVersion)
    {
        var now = DateTime.UtcNow;

        var row = await _context.CandidateDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (row == null)
        {
            row = new CandidateDeviceToken
            {
                DeviceTokenId = Guid.NewGuid(),
                Token = token,
                CreatedAt = now
            };
            _context.CandidateDeviceTokens.Add(row);
        }

        // If the token previously belonged to another candidate (shared phone,
        // logout without network) it moves to the current candidate.
        row.CandidateId = candidateId;
        row.Platform = platform;
        row.DeviceId = deviceId;
        row.AppVersion = appVersion?.Trim();
        row.UpdatedAt = now;
        row.LastUsedAt = now;

        // Firebase rotated the token for this install -> drop the old one.
        if (deviceId != null)
        {
            var superseded = await _context.CandidateDeviceTokens
                .Where(t => t.CandidateId == candidateId
                            && t.DeviceId == deviceId
                            && t.Token != token)
                .ToListAsync();

            if (superseded.Count > 0)
                _context.CandidateDeviceTokens.RemoveRange(superseded);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<DeviceTokenResponseDto> UnregisterAsync(
        Guid candidateId,
        UnregisterDeviceTokenRequestDto request)
    {
        try
        {
            var token = request.FcmToken.Trim();

            var rows = await _context.CandidateDeviceTokens
                .Where(t => t.CandidateId == candidateId && t.Token == token)
                .ToListAsync();

            if (rows.Count > 0)
            {
                _context.CandidateDeviceTokens.RemoveRange(rows);
                await _context.SaveChangesAsync();
            }

            // Idempotent: removing an unknown token is still a success.
            return new DeviceTokenResponseDto
            {
                Success = true,
                Message = "Device token removed."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnregisterAsync failed for candidate {CandidateId}", candidateId);
            return Fail("Internal server error.");
        }
    }

    private static DeviceTokenResponseDto Fail(string message)
        => new() { Success = false, Message = message };
}