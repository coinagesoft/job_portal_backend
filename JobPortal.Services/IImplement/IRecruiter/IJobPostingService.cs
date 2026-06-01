using global::JobPortal.Application.DTOs.Recruiter;
using JobPortal.Application.DTOs.JobPosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{


    public interface IJobPostingService
    {
        // Step 1 — creates draft job
        Task<JobDetailsResponseDto> SaveJobDetailsAsync(
            JobDetailsRequestDto request, Guid employerId);

        // Step 2-6 — update draft (all use jobId)
        Task<BaseJobResponseDto> SaveCompensationAsync(
            CompensationRequestDto request, Guid jobId, Guid employerId);

        Task<BaseJobResponseDto> SaveSkillsAsync(
            SkillsRequestDto request, Guid jobId, Guid employerId);

        Task<BaseJobResponseDto> SaveEligibilityAsync(
            EligibilityRequestDto request, Guid jobId, Guid employerId);

        Task<BaseJobResponseDto> SaveLocationAsync(
            LocationRequestDto request, Guid jobId, Guid employerId);

        Task<BaseJobResponseDto> SaveQuestionsAsync(
            QuestionsRequestDto request, Guid jobId, Guid employerId);

        // Step 7 — publish or save draft
        Task<PublishingResponseDto> PublishJobAsync(
            PublishingRequestDto request, Guid employerId);

        // Save draft at any step
        Task<BaseJobResponseDto> SaveDraftAsync(Guid jobId, Guid employerId);

        // Role search — no dropdown, free search
        Task<RoleSearchResponseDto> SearchRolesAsync(string query);

        // Resume incomplete job posting
        Task<ResumeJobResponseDto> ResumeJobAsync(Guid jobId, Guid employerId);
    }
}
