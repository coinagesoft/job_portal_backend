// ============================================================
//  JobPortal.Services/IImplement/IRecruiter/IRecruiterHomepageService.cs
// ============================================================

using JobPortal.Application.DTOs.Recruiter.Homepage;

namespace JobPortal.Services.IImplement.IRecruiter;

public interface IRecruiterHomepageService
{
    /// <summary>
    /// Active-only Industry Type options for Employer Registration Step 1
    /// (the GST check step), in admin-configured display order.
    /// Backs GET api/recruiter/registration/industries.
    /// </summary>
    Task<RecruiterIndustriesResponseDto> GetRegistrationIndustriesAsync();

    /// <summary>
    /// Active-only Trade/Role and Department options for the job posting
    /// form, in admin-configured display order.
    /// Backs GET api/recruiter/jobs/dropdowns.
    /// </summary>
    Task<RecruiterJobPostingDropdownsResponseDto> GetJobPostingDropdownsAsync();

    /// <summary>
    /// Records a "this isn't in your list" suggestion — from either the
    /// registration Industry Type field or the job posting Trade-Role /
    /// Department fields — for admin review. <paramref name="allowedFields"/>
    /// restricts which Field values the caller may submit (each controller
    /// passes only the field(s) that belong to it).
    /// </summary>
    Task<RecruiterSuggestionResponseDto> SubmitSuggestionAsync(
        RecruiterSuggestionRequestDto request,
        Guid? submittedByUserId,
        params string[] allowedFields);
}