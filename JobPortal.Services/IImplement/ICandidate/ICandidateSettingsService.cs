// ============================================================
//  JobPortal.Services/IImplement/ICandidate/
//  ICandidateSettingsService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Settings;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateSettingsService
{
    // ── Profile Preferences ──────────────────────────────────────────
    Task<CandidatePreferenceResponseDto> GetPreferencesAsync(Guid candidateId);
    Task<UpdateCandidatePreferenceResponseDto> UpdatePreferencesAsync(
        Guid candidateId, UpdateCandidatePreferenceRequestDto request);

    // ── Notification Preferences ─────────────────────────────────────
    Task<CandidateNotificationResponseDto> GetNotificationsAsync(Guid candidateId);
    Task<CandidateNotificationResponseDto> UpdateNotificationsAsync(
        Guid candidateId, UpdateCandidateNotificationRequestDto request);
    Task<CandidateNotificationResponseDto> ResetNotificationsAsync(Guid candidateId);

    // ── Help & Support ───────────────────────────────────────────────

    Task<CandidateCreateTicketResponseDto> CreateTicketAsync(
        Guid candidateId,
        CandidateCreateTicketRequestDto request);

    Task<CandidateTicketListResponseDto> GetTicketsAsync(
        Guid candidateId);

    Task<CandidateTicketDetailResponseDto> GetTicketByIdAsync(
        Guid candidateId,
        Guid ticketId);

    Task<CandidateTicketThreadResponseDto?> GetTicketThreadAsync(
        Guid candidateId,
        Guid ticketId);

    Task<CandidateAddReplyResponseDto> AddReplyAsync(
        Guid candidateId,
        Guid ticketId,
        CandidateAddReplyRequestDto request);

    Task<bool> ResolveTicketAsync(
        Guid ticketId);

    Task<CandidateTicketSummaryDto> GetSummaryAsync(
        Guid candidateId);
}