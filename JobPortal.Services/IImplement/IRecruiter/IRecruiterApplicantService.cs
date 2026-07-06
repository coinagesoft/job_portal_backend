using JobPortal.Application.DTOs.Recruiter.Applicants;

namespace JobPortal.Services.IImplement.IRecruiter;

public interface IRecruiterApplicantService
{
    // Dashboard
    Task<ApplicantDashboardResponseDto> GetDashboardAsync(
        Guid employerId);

    // Applicant List
    Task<ApplicantListResponseDto> GetApplicantsAsync(
        Guid employerId,
        ApplicantListRequestDto request);

    // Applicant Details
    Task<ApplicantDetailsResponseDto?> GetApplicantDetailsAsync(
        Guid employerId,
        Guid applicationId);

    // Job-wise Applicants
    Task<JobApplicantsResponseDto?> GetJobApplicantsAsync(
        Guid employerId,
        Guid jobId);

    // Status Updates
    Task<UpdateApplicantStatusResponseDto> MoveToReviewAsync(
        Guid employerId,
        Guid applicationId);

    Task<UpdateApplicantStatusResponseDto> ShortlistApplicantAsync(
        Guid employerId,
        Guid applicationId);

    Task<UpdateApplicantStatusResponseDto> ScheduleInterviewAsync(
        Guid employerId,
        Guid applicationId,
        ScheduleInterviewRequestDto request);

    Task<UpdateApplicantStatusResponseDto> RejectApplicantAsync(
        Guid employerId,
        Guid applicationId,
        RejectApplicantRequestDto request);

    Task<UpdateApplicantStatusResponseDto> HireApplicantAsync(
        Guid employerId,
        Guid applicationId);

    // Recruiter Notes
    Task<UpdateApplicantStatusResponseDto> AddRecruiterNoteAsync(
        Guid employerId,
        Guid applicationId,
        AddRecruiterNoteRequestDto request);

    Task<RecruiterNotesResponseDto> GetRecruiterNotesAsync(
        Guid employerId,
        Guid applicationId);
}