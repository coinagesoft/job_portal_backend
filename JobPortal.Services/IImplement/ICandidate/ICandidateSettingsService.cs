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
    Task<CreateSupportTicketResponseDto> CreateTicketAsync(
        Guid candidateId, CreateSupportTicketRequestDto request);
    Task<SupportTicketListResponseDto> GetTicketsAsync(Guid candidateId);
    Task<SupportTicketDetailResponseDto> GetTicketByIdAsync(
        Guid candidateId, Guid ticketId);
}
