
// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateLogoutService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Missing;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateLogoutService : ICandidateLogoutService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateLogoutService> _logger;

    public CandidateLogoutService(
        AppDbContext context,
        ILogger<CandidateLogoutService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CandidateLogoutResponseDto> LogoutAsync(
        Guid candidateId,
        CandidateLogoutRequestDto request,
        string? jwtJti,
        DateTime? jwtExpiresAt)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return Fail("Candidate profile not found.");

            // 1. Clear FCM token on the profile so no more push notifications
            if (!string.IsNullOrEmpty(request.FcmToken)
                && profile.FcmToken == request.FcmToken)
            {
                profile.FcmToken = null;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Record the logout session for JWT blacklisting
            var session = new CandidateLogoutSession
            {
                LogoutSessionId = Guid.NewGuid(),
                CandidateId = candidateId,
                FcmToken = request.FcmToken,
                JwtJti = jwtJti,
                LoggedOutAt = DateTime.UtcNow,
                JwtExpiresAt = jwtExpiresAt
            };

            _context.CandidateLogoutSessions.Add(session);
            await _context.SaveChangesAsync();

            return new CandidateLogoutResponseDto
            {
                Success = true,
                Message = "Logged out successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogoutAsync failed for {Id}", candidateId);
            return Fail("Internal server error during logout.");
        }
    }

    private static CandidateLogoutResponseDto Fail(string msg)
        => new() { Success = false, Message = msg };
}