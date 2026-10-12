// ============================================================
//  JobPortal.Services/Implement/Candidate/
//  CandidateSettingsService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateSettingsService : ICandidateSettingsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateSettingsService> _logger;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly ISubUserEmailService _emailService;

    private const string DeleteOtpPurpose = "CandidateDeleteAccount";
    private const int DeleteOtpExpiryMinutes = 10;
    private const int DeleteOtpMaxAttempts = 3;
    private const int DeleteOtpResendCooldownSeconds = 30;

    public CandidateSettingsService(
        AppDbContext context,
        ILogger<CandidateSettingsService> logger,
        ITwilioOtpService twilioOtpService,
        ISubUserEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _twilioOtpService = twilioOtpService;
        _emailService = emailService;
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

    // ════════════════════════════════════════════════════════════════
    // DELETE ACCOUNT — which channels can the candidate verify with?
    // ════════════════════════════════════════════════════════════════

    public async Task<CandidateDeleteAccountOptionsDto> GetDeleteAccountOptionsAsync(
        Guid candidateId)
    {
        try
        {
            var user = await _context.CandidateProfiles
                .AsNoTracking()
                .Where(p => p.CandidateId == candidateId)
                .Select(p => p.User)
                .FirstOrDefaultAsync();

            if (user == null)
                return new CandidateDeleteAccountOptionsDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            var mobile = user.MobileNumber?.Trim();
            var email = user.Email?.Trim();

            var hasMobile = !string.IsNullOrWhiteSpace(mobile) &&
                            !string.IsNullOrWhiteSpace(user.CountryCode);
            var hasEmail = !string.IsNullOrWhiteSpace(email);

            return new CandidateDeleteAccountOptionsDto
            {
                Success = hasMobile || hasEmail,
                Message = hasMobile || hasEmail
                    ? string.Empty
                    : "No mobile number or email on this account to send an OTP to.",
                HasMobile = hasMobile,
                MaskedMobile = hasMobile ? MaskMobile(mobile!) : null,
                HasEmail = hasEmail,
                MaskedEmail = hasEmail ? MaskEmail(email!) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeleteAccountOptionsAsync error for candidateId={Id}", candidateId);
            return new CandidateDeleteAccountOptionsDto
            {
                Success = false,
                Message = "Unable to load verification options."
            };
        }
    }

    // ════════════════════════════════════════════════════════════════
    // DELETE ACCOUNT — STEP 1: send OTP to the ONE chosen channel
    // ════════════════════════════════════════════════════════════════

    public async Task<CandidateDeleteAccountOtpResponseDto> SendDeleteAccountOtpAsync(
        Guid candidateId,
        string channel)
    {
        try
        {
            var chosen = channel?.Trim().ToLowerInvariant();

            if (chosen != "mobile" && chosen != "email")
                return DeleteOtpFail("Please choose mobile or email to receive the OTP.");

            var useEmail = chosen == "email";

            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            var user = profile?.User;

            if (profile == null || user == null)
                return DeleteOtpFail("Candidate profile not found.");

            if (user.UserType != UserType.Candidate)
                return DeleteOtpFail("Only candidate accounts can be deleted from here.");

            var mobile = user.MobileNumber?.Trim();
            var countryCode = user.CountryCode?.Trim();
            var email = user.Email?.Trim().ToLower();

            if (useEmail && string.IsNullOrWhiteSpace(email))
                return DeleteOtpFail("No email on this account. Please choose mobile instead.");

            if (!useEmail && (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(countryCode)))
                return DeleteOtpFail("No mobile number on this account. Please choose email instead.");

            // Resend cooldown + retire older OTPs
            var activeOtps = await _context.OtpVerifications
                .Where(o =>
                    o.UserId == user.UserId &&
                    o.Purpose == DeleteOtpPurpose &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .ToListAsync();

            var recent = activeOtps.FirstOrDefault();
            if (recent != null)
            {
                var cooldownEnd = recent.OtpSentAt.AddSeconds(recent.ResendCooldownSec);
                if (DateTime.UtcNow < cooldownEnd)
                {
                    var wait = Math.Max(1, (int)(cooldownEnd - DateTime.UtcNow).TotalSeconds);
                    return DeleteOtpFail($"Please wait {wait} seconds before requesting a new OTP.");
                }

                foreach (var old in activeOtps)
                    old.IsVerified = true;   // retire previous codes

                await _context.SaveChangesAsync();
            }

            string masked;

            if (useEmail)
            {
                var otpCode = GenerateOtp();

                await _emailService.SendOtpEmailAsync(email!, otpCode);

                _context.OtpVerifications.Add(new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = email,
                    Email = email,
                    CountryCode = "email",
                    OtpCode = BCrypt.Net.BCrypt.HashPassword(otpCode),
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(DeleteOtpExpiryMinutes),
                    ResendCooldownSec = DeleteOtpResendCooldownSeconds,
                    IsVerified = false,
                    Purpose = DeleteOtpPurpose
                });

                masked = MaskEmail(email!);
            }
            else
            {
                var sent = await _twilioOtpService.SendOtpAsync($"{countryCode}{mobile}");

                if (!sent)
                    return DeleteOtpFail("Unable to send OTP. Please try again.");

                _context.OtpVerifications.Add(new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = mobile,
                    CountryCode = countryCode,
                    OtpCode = "TWILIO_VERIFY",
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(DeleteOtpExpiryMinutes),
                    ResendCooldownSec = DeleteOtpResendCooldownSeconds,
                    IsVerified = false,
                    Purpose = DeleteOtpPurpose
                });

                masked = MaskMobile(mobile!);
            }

            await _context.SaveChangesAsync();

            return new CandidateDeleteAccountOtpResponseDto
            {
                Success = true,
                Message = $"OTP sent to {masked}. Valid for {DeleteOtpExpiryMinutes} minutes.",
                MaskedIdentifier = masked,
                MaskedMobile = useEmail ? null : masked,
                MaskedEmail = useEmail ? masked : null,
                IdentifierType = useEmail ? "email" : "mobile",
                ExpiresInSeconds = DeleteOtpExpiryMinutes * 60,
                ResendCooldownSeconds = DeleteOtpResendCooldownSeconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendDeleteAccountOtpAsync error for candidateId={Id}", candidateId);
            return DeleteOtpFail("We couldn't send the OTP right now. Please try again in a few minutes.");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // DELETE ACCOUNT — STEP 2: verify OTP, then HARD delete
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies the OTP on the channel the candidate chose (mobile OR email)
    /// and only then permanently deletes the candidate: every candidate row
    /// is purged and the User row itself is removed in one transaction.
    /// Because the User row is gone, the same mobile number / email can be
    /// used to register a brand-new account straight away, with no
    /// "already registered" or "suspended" message. Any JWT that is still
    /// unexpired is rejected by ActiveSubUserMiddleware (user no longer
    /// exists).
    /// </summary>
    public async Task<CandidateSettingsDeleteAccountResponseDto> DeleteAccountAsync(
        Guid candidateId,
        CandidateSettingsDeleteAccountRequestDto request)
    {
        try
        {
            var chosen = request.Channel?.Trim().ToLowerInvariant();

            if (chosen != "mobile" && chosen != "email")
                return DeleteFail("Please choose mobile or email to verify with.");

            if (string.IsNullOrWhiteSpace(request.OtpCode))
                return DeleteFail("Please enter the OTP.");

            var useEmail = chosen == "email";

            // AsNoTracking: the rows are removed with ExecuteDelete below.
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return DeleteFail("Candidate profile not found.");

            var user = profile.User;

            if (user == null)
                return DeleteFail("User not found.");

            if (user.UserType != UserType.Candidate)
                return DeleteFail("Only candidate accounts can be deleted from here.");

            if (await _context.EmployerProfiles.AnyAsync(e => e.UserId == user.UserId))
                return DeleteFail("This account also has an employer profile and cannot be deleted from here.");

            // ── OTP gate: nothing below runs unless the OTP is valid ──
            var otp = await _context.OtpVerifications
                .Where(o =>
                    o.UserId == user.UserId &&
                    o.Purpose == DeleteOtpPurpose &&
                    !o.IsVerified &&
                    (useEmail ? o.CountryCode == "email" : o.CountryCode != "email"))
                .OrderByDescending(o => o.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return DeleteFail("OTP not found. Please request a new OTP.");

            if (DateTime.UtcNow > otp.OtpExpiresAt)
                return DeleteFail("OTP has expired. Please request a new one.");

            if (otp.OtpAttempts >= DeleteOtpMaxAttempts)
                return DeleteFail("Too many failed attempts. Please request a new OTP.");

            var code = request.OtpCode.Trim();
            bool otpValid;

            if (otp.OtpCode == "TWILIO_VERIFY")
            {
                try
                {
                    otpValid = await _twilioOtpService.VerifyOtpAsync(
                        $"{otp.CountryCode}{otp.MobileNumber}", code);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Delete-account OTP verify call failed for candidateId={Id}", candidateId);
                    otpValid = false;
                }
            }
            else
            {
                otpValid = BCrypt.Net.BCrypt.Verify(code, otp.OtpCode);
            }

            if (!otpValid)
            {
                otp.OtpAttempts++;
                await _context.SaveChangesAsync();

                var remaining = DeleteOtpMaxAttempts - otp.OtpAttempts;

                return DeleteFail(
                    remaining > 0
                        ? $"Invalid OTP. {remaining} attempt(s) remaining."
                        : "Too many failed attempts. Please request a new OTP.");
            }

            // ── OTP verified → permanent delete ──
            var userId = user.UserId;

            await using var tx = await _context.Database.BeginTransactionAsync();

            _context.ChangeTracker.Clear();

            await AccountCleanupService.PurgeCandidateDataAsync(
                _context, profile, user, CancellationToken.None);

            await _context.Users
                .Where(u => u.UserId == userId)
                .ExecuteDeleteAsync();

            await tx.CommitAsync();

            _logger.LogInformation(
                "Candidate permanently deleted own account. CandidateId={CandidateId} UserId={UserId} Via={Channel} Reason={Reason}",
                candidateId, userId, chosen, string.IsNullOrWhiteSpace(request.Reason) ? "(none)" : request.Reason.Trim());

            return new CandidateSettingsDeleteAccountResponseDto
            {
                Success = true,
                Message = "Your account and all related data have been permanently deleted. You can register again with the same mobile number or email.",
                RecoveryExpiry = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAccountAsync error for candidateId={Id}", candidateId);
            return DeleteFail("An error occurred while deleting your account. No changes were made.");
        }
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

    private static CandidateDeleteAccountOtpResponseDto DeleteOtpFail(string msg) =>
        new() { Success = false, Message = msg };

    private static string GenerateOtp()
    {
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
    }

    private static string MaskMobile(string mobile) =>
        mobile.Length <= 4
            ? "****"
            : new string('*', mobile.Length - 4) + mobile[^4..];

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        var name = parts[0];
        var masked = name.Length <= 2
            ? name[0] + "***"
            : name[..2] + new string('*', name.Length - 2);
        return parts.Length > 1 ? $"{masked}@{parts[1]}" : masked;
    }

    private static CandidateSettingsDeleteAccountResponseDto DeleteFail(string msg) =>
        new() { Success = false, Message = msg };
}