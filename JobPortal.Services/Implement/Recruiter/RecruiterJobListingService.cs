using JobPortal.Application.DTOs.Recruiter.JobListing;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterJobListingService
        : IRecruiterJobListingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterJobListingService> _logger;

        public RecruiterJobListingService(
            AppDbContext context,
            ILogger<RecruiterJobListingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<JobDashboardResponseDto>
            GetDashboardAsync(Guid employerId)
        {
            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .ToListAsync();

            return new JobDashboardResponseDto
            {
                TotalJobs = jobs.Count,

                ActiveJobs =
                    jobs.Count(x =>
                        x.JobStatus == JobStatus.Active),

                PausedJobs =
                    jobs.Count(x =>
                        x.JobStatus == JobStatus.Paused),

                ClosedJobs =
                    jobs.Count(x =>
                        x.JobStatus == JobStatus.Closed),

                ArchivedJobs =
                    jobs.Count(x =>
                        x.JobStatus == JobStatus.Archived),

                NormalJobs =
                    jobs.Count(x =>
                        x.JobType == JobType.Normal),

                ClassifiedJobs =
                    jobs.Count(x =>
                        x.JobType == JobType.Classified),

                HotVacancyJobs =
                    jobs.Count(x =>
                        x.JobType == JobType.HotVacancy)
            };
        }

        public async Task<JobListResponseDto>
            GetJobsAsync(
                Guid employerId,
                JobListRequestDto request)
        {
            var query =
                _context.JobPostings
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                request.Search))
            {
                query = query.Where(x =>
                    x.JobTitle.Contains(request.Search) ||
                    x.TradeCategory.Contains(request.Search));
            }

            if (!string.IsNullOrWhiteSpace(
                request.Status))
            {
                if (Enum.TryParse<JobStatus>(
                    request.Status,
                    true,
                    out var status))
                {
                    query = query.Where(x =>
                        x.JobStatus == status);
                }
            }

            if (request.JobType.HasValue)
            {
                query = query.Where(x =>
                    x.JobType == request.JobType.Value);
            }

            var totalRecords =
                await query.CountAsync();

            var jobs =
                await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(
                    (request.PageNumber - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .Select(x =>
                    new RecruiterJobListItemDto
                    {
                        JobId = x.JobId,

                        JobTitle = x.JobTitle,

                        TradeCategory =
                            x.TradeCategory,

                        Role =
                            x.Role,

                        JobType =
                            x.JobType,

                        JobStatus =
                            x.JobStatus.ToString(),

                        AppliedCount =
                            x.AppliedCount,

                        Vacancies =
                            x.Vacancies,

                        SalaryMin =
                            x.SalaryMin,

                        SalaryMax =
                            x.SalaryMax,

                        ApplicationDeadline =
                            x.ApplicationDeadline,

                        CreatedAt =
                            x.CreatedAt,

                        PublishedAt =
                            x.PublishedAt,

                        Location =
                            x.LocationType == "Onshore"
                                ? $"{x.OnshoreCity}, {x.OnshoreState}"
                                : x.OffshoreRegion ?? ""
                    })
                .ToListAsync();

            return new JobListResponseDto
            {
                TotalRecords = totalRecords,

                PageNumber =
                    request.PageNumber,

                PageSize =
                    request.PageSize,

                Jobs = jobs
            };
        }

        public async Task<RecruiterJobDetailResponseDto?>
            GetJobByIdAsync(
                Guid employerId,
                Guid jobId)
        {
            var job =
                await _context.JobPostings
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId);

            if (job == null)
            {
                return null;
            }

            return new RecruiterJobDetailResponseDto
            {
                JobId = job.JobId,

                JobTitle = job.JobTitle,

                JobDescription =
                    job.JobDescription,

                TradeCategory =
                    job.TradeCategory,

                Role =
                    job.Role,

                JobType =
                    job.JobType,

                JobStatus =
                    job.JobStatus.ToString(),

                SalaryMin =
                    job.SalaryMin,

                SalaryMax =
                    job.SalaryMax,

                SalaryCurrency =
                    job.SalaryCurrency,

                Vacancies =
                    job.Vacancies,

                ExperienceRequiredYears =
                    job.ExperienceRequiredYears,

                EducationRequired =
                    job.EducationRequired,

                LanguageRequired =
                    job.LanguageRequired,

                LicenceDocsRequired =
                    job.LicenceDocsRequired,

                KeySkills =
                    job.KeySkills,

                LocationType =
                    job.LocationType,

                OnshoreCity =
                    job.OnshoreCity,

                OnshoreState =
                    job.OnshoreState,

                OffshoreVesselName =
                    job.OffshoreVesselName,

                OffshoreRegion =
                    job.OffshoreRegion,

                PassportRequired =
                    job.PassportRequired,

                ApplicationDeadline =
                    job.ApplicationDeadline,

                AppliedCount =
                    job.AppliedCount,

                CreatedAt =
                    job.CreatedAt,

                PublishedAt =
                    job.PublishedAt
            };
        }

        public async Task<JobStatusUpdateResponseDto> PauseJobAsync(
        Guid employerId,
        Guid jobId)
        {
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId);

            if (job == null)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = "Job not found."
                };
            }

            job.JobStatus = JobStatus.Paused;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new JobStatusUpdateResponseDto
            {
                Success = true,
                Message = "Job paused successfully.",
                JobId = job.JobId,
                JobStatus = job.JobStatus.ToString()
            };
        }

        public async Task<JobStatusUpdateResponseDto>
    ResumeJobAsync(
        Guid employerId,
        Guid jobId)
        {
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId);

            if (job == null)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = "Job not found."
                };
            }

            job.JobStatus = JobStatus.Active;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new JobStatusUpdateResponseDto
            {
                Success = true,
                Message = "Job resumed successfully.",
                JobId = job.JobId,
                JobStatus = job.JobStatus.ToString()
            };
        }
        public async Task<JobStatusUpdateResponseDto>
    CloseJobAsync(
        Guid employerId,
        Guid jobId)
        {
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId);

            if (job == null)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = "Job not found."
                };
            }

            job.JobStatus = JobStatus.Closed;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new JobStatusUpdateResponseDto
            {
                Success = true,
                Message = "Job closed successfully.",
                JobId = job.JobId,
                JobStatus = job.JobStatus.ToString()
            };
        }

        public async Task<JobStatusUpdateResponseDto>
    ArchiveJobAsync(
        Guid employerId,
        Guid jobId)
        {
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId);

            if (job == null)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = "Job not found."
                };
            }

            job.JobStatus = JobStatus.Archived;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new JobStatusUpdateResponseDto
            {
                Success = true,
                Message = "Job archived successfully.",
                JobId = job.JobId,
                JobStatus = job.JobStatus.ToString()
            };
        }

        public async Task<JobStatsResponseDto> GetJobStatsAsync(
        Guid employerId,
        Guid jobId)
        {
            var applications =
                await _context.JobApplications
                .AsNoTracking()
                .Where(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId)
                .ToListAsync();

            return new JobStatsResponseDto
            {
                TotalApplications =
                    applications.Count,

                Applied =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Applied"),

                InReview =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "InReview"),

                Shortlisted =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Shortlisted"),

                Interview =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Interview"),

                Rejected =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Rejected"),

                Hired =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Hired"),

                Withdrawn =
                    applications.Count(x =>
                        x.ApplicationStatus.ToString() == "Withdrawn")
            };
        }
    }
}