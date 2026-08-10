// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateSettingsService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
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

    public async Task<CandidateCreateTicketResponseDto> CreateTicketAsync(
        Guid candidateId, CandidateCreateTicketRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new CandidateCreateTicketResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

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

            return new CandidateCreateTicketResponseDto
            {
                Success = true,
                Message = "Support ticket submitted successfully.",
                TicketId = ticket.TicketId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTicketAsync error for candidateId={Id}", candidateId);
            return new CandidateCreateTicketResponseDto
            {
                Success = false,
                Message = "An error occurred while submitting the support ticket."
            };
        }
    }

    public async Task<CandidateTicketListResponseDto> GetTicketsAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new CandidateTicketListResponseDto();

            var tickets = await _context.SupportTickets
                .Where(t => t.RaisedBy == profile.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return new CandidateTicketListResponseDto
            {
                TotalTickets = tickets.Count,
                Tickets = tickets.Select(MapToItemDto).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTicketsAsync error for candidateId={Id}", candidateId);
            return new CandidateTicketListResponseDto();
        }
    }

    public async Task<CandidateTicketDetailResponseDto> GetTicketByIdAsync(
        Guid candidateId, Guid ticketId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new CandidateTicketDetailResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            var ticket = await _context.SupportTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TicketId == ticketId &&
                    t.RaisedBy == profile.UserId);

            if (ticket == null)
                return new CandidateTicketDetailResponseDto
                {
                    Success = false,
                    Message = "Ticket not found."
                };

            return new CandidateTicketDetailResponseDto
            {
                Success = true,
                Message = "Ticket retrieved successfully.",
                Data = MapToItemDto(ticket)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetTicketByIdAsync error for candidateId={Id}, ticketId={Tid}",
                candidateId, ticketId);
            return new CandidateTicketDetailResponseDto
            {
                Success = false,
                Message = "An error occurred while retrieving the ticket."
            };
        }
    }

    public async Task<CandidateTicketThreadResponseDto?> GetTicketThreadAsync(
        Guid candidateId, Guid ticketId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return null;

            var ticket = await _context.SupportTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TicketId == ticketId &&
                    t.RaisedBy == profile.UserId);

            if (ticket == null)
                return null;

            var replies = await _context.SupportTicketReplies
                .Where(x => x.TicketId == ticketId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new CandidateTicketReplyDto
                {
                    ReplyId = x.ReplyId,
                    Message = x.Message,
                    SenderType = x.SenderType.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new CandidateTicketThreadResponseDto
            {
                Success = true,
                Message = "Thread retrieved successfully.",
                TicketId = ticket.TicketId,
                Subject = ticket.Subject,
                Category = ticket.TicketType,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                ResolutionNote = ticket.ResolutionNote,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                Replies = replies
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetTicketThreadAsync error for candidateId={Id}, ticketId={TicketId}",
                candidateId, ticketId);
            return null;
        }
    }

    public async Task<CandidateAddReplyResponseDto> AddReplyAsync(
        Guid candidateId, Guid ticketId, CandidateAddReplyRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new CandidateAddReplyResponseDto
                {
                    Success = false,
                    Message = "Candidate not found."
                };

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t =>
                    t.TicketId == ticketId &&
                    t.RaisedBy == profile.UserId);

            if (ticket == null)
                return new CandidateAddReplyResponseDto
                {
                    Success = false,
                    Message = "Ticket not found."
                };

            if (ticket.Status == "Resolved")
                return new CandidateAddReplyResponseDto
                {
                    Success = false,
                    Message = "This ticket is resolved and no longer accepts new messages. Please raise a new ticket if you need further help."
                };

            var reply = new SupportTicketReply
            {
                ReplyId = Guid.NewGuid(),
                TicketId = ticketId,
                SenderId = profile.UserId,
                SenderType = ReplySenderType.Candidate,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTicketReplies.Add(reply);
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CandidateAddReplyResponseDto
            {
                Success = true,
                Message = "Reply added successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddReplyAsync error for candidateId={Id}, ticketId={TicketId}",
                candidateId, ticketId);
            return new CandidateAddReplyResponseDto
            {
                Success = false,
                Message = "An error occurred while adding the reply."
            };
        }
    }

    public async Task<bool> ResolveTicketAsync(Guid ticketId)
    {
        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(x => x.TicketId == ticketId);

        if (ticket == null)
            return false;

        ticket.Status = "Resolved";
        ticket.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<CandidateTicketSummaryDto> GetSummaryAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new CandidateTicketSummaryDto();

            var tickets = await _context.SupportTickets
                .Where(x => x.RaisedBy == profile.UserId)
                .ToListAsync();

            return new CandidateTicketSummaryDto
            {
                TotalTickets = tickets.Count,
                Open = tickets.Count(x => x.Status == "Open"),
                InProgress = tickets.Count(x => x.Status == "InProgress"),
                Resolved = tickets.Count(x => x.Status == "Resolved")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSummaryAsync error for candidateId={Id}", candidateId);
            return new CandidateTicketSummaryDto();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════

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

    private static CandidateTicketItemDto MapToItemDto(SupportTicket t) =>
        new()
        {
            TicketId = t.TicketId,
            Subject = t.Subject,
            Category = t.TicketType,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
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