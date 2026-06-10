// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateSettingsService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateSettingsService : ICandidateSettingsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateSettingsService> _logger;

    public CandidateSettingsService(
        AppDbContext context,
        ILogger<CandidateSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════
    // PROFILE PREFERENCES
    // ════════════════════════════════════════════════════════════════

    public async Task<CandidatePreferenceResponseDto> GetPreferencesAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return PrefFail("Candidate profile not found.");

            var pref = await _context.CandidatePreferenceSettings
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            // Auto-create defaults on first access
            if (pref == null)
            {
                pref = new CandidatePreferenceSetting
                {
                    PrefId = Guid.NewGuid(),
                    CandidateId = candidateId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CandidatePreferenceSettings.Add(pref);
                await _context.SaveChangesAsync();
            }

            return new CandidatePreferenceResponseDto
            {
                Success = true,
                Message = "Preferences retrieved successfully.",
                Data = MapToPreferenceData(pref, profile)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPreferencesAsync error for candidateId={Id}", candidateId);
            return PrefFail("An error occurred while retrieving preferences.");
        }
    }

    public async Task<UpdateCandidatePreferenceResponseDto> UpdatePreferencesAsync(
        Guid candidateId, UpdateCandidatePreferenceRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return UpdatePrefFail("Candidate profile not found.");

            var pref = await _context.CandidatePreferenceSettings
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (pref == null)
            {
                pref = new CandidatePreferenceSetting
                {
                    PrefId = Guid.NewGuid(),
                    CandidateId = candidateId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CandidatePreferenceSettings.Add(pref);
            }

            pref.PreferredLanguage = request.PreferredLanguage;
            pref.TimeZone = request.TimeZone;
            pref.ResumeVisibility = request.ResumeVisibility;
            pref.CommunicationPreference = request.CommunicationPreference;
            pref.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UpdateCandidatePreferenceResponseDto
            {
                Success = true,
                Message = "Preferences updated successfully.",
                Data = MapToPreferenceData(pref, profile)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePreferencesAsync error for candidateId={Id}", candidateId);
            return UpdatePrefFail("An error occurred while updating preferences.");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // NOTIFICATION PREFERENCES
    // ════════════════════════════════════════════════════════════════

    public async Task<CandidateNotificationResponseDto> GetNotificationsAsync(Guid candidateId)
    {
        try
        {
            var notif = await GetOrCreateNotifSettingAsync(candidateId);
            if (notif == null)
                return NotifFail("Candidate profile not found.");

            return new CandidateNotificationResponseDto
            {
                Success = true,
                Message = "Notification preferences retrieved.",
                Data = MapToNotifData(candidateId, notif)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNotificationsAsync error for candidateId={Id}", candidateId);
            return NotifFail("An error occurred while retrieving notification preferences.");
        }
    }

    public async Task<CandidateNotificationResponseDto> UpdateNotificationsAsync(
        Guid candidateId, UpdateCandidateNotificationRequestDto request)
    {
        try
        {
            var notif = await GetOrCreateNotifSettingAsync(candidateId);
            if (notif == null)
                return NotifFail("Candidate profile not found.");

            notif.JobMatches = request.JobMatches;
            notif.ApplicationUpdates = request.ApplicationUpdates;
            notif.RecruiterMessages = request.RecruiterMessages;
            notif.DocumentReminders = request.DocumentReminders;
            notif.OffersAnnouncements = request.OffersAnnouncements;
            notif.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CandidateNotificationResponseDto
            {
                Success = true,
                Message = "Notification preferences saved.",
                Data = MapToNotifData(candidateId, notif)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateNotificationsAsync error for candidateId={Id}", candidateId);
            return NotifFail("An error occurred while saving notification preferences.");
        }
    }

    public async Task<CandidateNotificationResponseDto> ResetNotificationsAsync(Guid candidateId)
    {
        try
        {
            var notif = await GetOrCreateNotifSettingAsync(candidateId);
            if (notif == null)
                return NotifFail("Candidate profile not found.");

            // Reset all toggles to their defaults (all ON)
            notif.JobMatches = true;
            notif.ApplicationUpdates = true;
            notif.RecruiterMessages = true;
            notif.DocumentReminders = true;
            notif.OffersAnnouncements = true;
            notif.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CandidateNotificationResponseDto
            {
                Success = true,
                Message = "Notification preferences reset to defaults.",
                Data = MapToNotifData(candidateId, notif)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetNotificationsAsync error for candidateId={Id}", candidateId);
            return NotifFail("An error occurred while resetting notification preferences.");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // HELP & SUPPORT
    // ════════════════════════════════════════════════════════════════

    public async Task<CreateSupportTicketResponseDto> CreateTicketAsync(
        Guid candidateId, CreateSupportTicketRequestDto request)
    {
        try
        {
            // Verify candidate exists
            var profileExists = await _context.CandidateProfiles
                .Include(p => p.User)
                .AnyAsync(p => p.CandidateId == candidateId);

            if (!profileExists)
                return new CreateSupportTicketResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            // Resolve the UserId for SupportTicket.RaisedBy
            var profile = await _context.CandidateProfiles
                .FirstAsync(p => p.CandidateId == candidateId);

            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid(),
                RaisedBy = profile.UserId,
                TicketType = request.Category,
                Subject = request.Subject,
                Description = request.Description,
                Status = "Open",
                Priority = "Normal",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return new CreateSupportTicketResponseDto
            {
                Success = true,
                Message = "Support ticket submitted successfully.",
                Data = MapToTicketDto(ticket)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTicketAsync error for candidateId={Id}", candidateId);
            return new CreateSupportTicketResponseDto
            {
                Success = false,
                Message = "An error occurred while submitting the support ticket."
            };
        }
    }

    public async Task<SupportTicketListResponseDto> GetTicketsAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new SupportTicketListResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            var tickets = await _context.SupportTickets
                .Where(t => t.RaisedBy == profile.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return new SupportTicketListResponseDto
            {
                Success = true,
                Message = "Tickets retrieved successfully.",
                TotalTickets = tickets.Count,
                Tickets = tickets.Select(MapToTicketDto).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTicketsAsync error for candidateId={Id}", candidateId);
            return new SupportTicketListResponseDto
            {
                Success = false,
                Message = "An error occurred while retrieving tickets."
            };
        }
    }

    public async Task<SupportTicketDetailResponseDto> GetTicketByIdAsync(
        Guid candidateId, Guid ticketId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new SupportTicketDetailResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.TicketId == ticketId
                                       && t.RaisedBy == profile.UserId);

            if (ticket == null)
                return new SupportTicketDetailResponseDto
                {
                    Success = false,
                    Message = "Ticket not found."
                };

            return new SupportTicketDetailResponseDto
            {
                Success = true,
                Message = "Ticket retrieved successfully.",
                Data = MapToTicketDto(ticket)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTicketByIdAsync error for candidateId={Id}, ticketId={Tid}",
                candidateId, ticketId);
            return new SupportTicketDetailResponseDto
            {
                Success = false,
                Message = "An error occurred while retrieving the ticket."
            };
        }
    }

    // ════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════

    /// <summary>Returns existing notification setting row, or creates defaults.</summary>
    private async Task<CandidateNotificationSetting?> GetOrCreateNotifSettingAsync(Guid candidateId)
    {
        var profileExists = await _context.CandidateProfiles
            .AnyAsync(p => p.CandidateId == candidateId);

        if (!profileExists) return null;

        var notif = await _context.CandidateNotificationSettings
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (notif == null)
        {
            notif = new CandidateNotificationSetting
            {
                NotifPrefId = Guid.NewGuid(),
                CandidateId = candidateId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.CandidateNotificationSettings.Add(notif);
            await _context.SaveChangesAsync();
        }

        return notif;
    }

    private static CandidatePreferenceData MapToPreferenceData(
        CandidatePreferenceSetting pref, CandidateProfile profile)
    {
        return new CandidatePreferenceData
        {
            CandidateId = profile.CandidateId,
            PreferredLanguage = pref.PreferredLanguage,
            TimeZone = pref.TimeZone,
            ResumeVisibility = pref.ResumeVisibility,
            CommunicationPreference = pref.CommunicationPreference,
            TwoFactorEnabled = pref.TwoFactorEnabled,
            LastPasswordUpdatedAt = pref.LastPasswordUpdatedAt,
            LastLoginAt = profile.User?.LastLoginAt,
            PlanName = "Candidate"
        };
    }

    private static CandidateNotificationData MapToNotifData(
        Guid candidateId, CandidateNotificationSetting notif)
    {
        var enabled = new[]
        {
            notif.JobMatches, notif.ApplicationUpdates, notif.RecruiterMessages,
            notif.DocumentReminders, notif.OffersAnnouncements
        }.Count(v => v);

        return new CandidateNotificationData
        {
            CandidateId = candidateId,
            JobMatches = notif.JobMatches,
            ApplicationUpdates = notif.ApplicationUpdates,
            RecruiterMessages = notif.RecruiterMessages,
            DocumentReminders = notif.DocumentReminders,
            OffersAnnouncements = notif.OffersAnnouncements,
            EnabledCount = enabled,
            TotalCount = 5
        };
    }

    private static SupportTicketItemDto MapToTicketDto(SupportTicket t) =>
        new()
        {
            TicketId = t.TicketId,
            Subject = t.Subject,
            Category = t.TicketType,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            ResolutionNote = t.ResolutionNote,
            CreatedAt = t.CreatedAt,
            ResolvedAt = t.ResolvedAt
        };

    // Failure helpers
    private static CandidatePreferenceResponseDto PrefFail(string msg) =>
        new() { Success = false, Message = msg };

    private static UpdateCandidatePreferenceResponseDto UpdatePrefFail(string msg) =>
        new() { Success = false, Message = msg };

    private static CandidateNotificationResponseDto NotifFail(string msg) =>
        new() { Success = false, Message = msg };
}
