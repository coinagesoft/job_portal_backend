using JobPortal.Application.DTOs.Recruiter.JobListing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterJobListingService
    {
        Task<JobDashboardResponseDto>
    GetDashboardAsync(Guid employerId);

        Task<JobListResponseDto>
            GetJobsAsync(
                Guid employerId,
                JobListRequestDto request);

        Task<RecruiterJobDetailResponseDto?>
            GetJobByIdAsync(
                Guid employerId,
                Guid jobId);

        Task<JobStatusUpdateResponseDto>
            PauseJobAsync(
                Guid employerId,
                Guid jobId,
                Guid actionUserId,
                bool isSubUser);

        Task<JobStatusUpdateResponseDto>
            ResumeJobAsync(
                Guid employerId,
                Guid jobId,
                Guid actionUserId,
                bool isSubUser);

        Task<JobStatusUpdateResponseDto>
            CloseJobAsync(
                Guid employerId,
                Guid jobId,
                Guid actionUserId,
                bool isSubUser);

        Task<JobStatusUpdateResponseDto> ArchiveJobAsync(
                Guid employerId,
                Guid jobId,
                Guid actionUserId,
                bool isSubUser);

        Task<JobStatsResponseDto> GetJobStatsAsync(
        Guid employerId,
        Guid jobId);
    }
}