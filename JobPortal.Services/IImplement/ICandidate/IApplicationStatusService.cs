// ============================================================
//  JobPortal.Services/IImplement/ICandidate/IApplicationStatusService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Applications;

namespace JobPortal.Services.IImplement.ICandidate;

public interface IApplicationStatusService
{
    Task<ApplicationStatusResponseDto> GetApplicationStatusAsync(
        Guid candidateId, ApplicationStatusFilterDto filter);

    Task<AcknowledgeNoteResponseDto> AcknowledgeRecruiterNoteAsync(
        Guid applicationId, Guid candidateId);
}