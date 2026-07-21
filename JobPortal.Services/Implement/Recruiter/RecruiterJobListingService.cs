using JobPortal.Application.DTOs.JobPosting;
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
        private readonly ISubUserPermissionService _permissionService;

        public RecruiterJobListingService(
            AppDbContext context,
            ILogger<RecruiterJobListingService> logger,
            ISubUserPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task<JobDashboardResponseDto> GetDashboardAsync(Guid employerId)
        {
            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId &&
                    !x.IsDeleted)
                .ToListAsync();

            return new JobDashboardResponseDto
            {
                // =====================================================
                // Overall
                // =====================================================

                TotalJobs = jobs.Count,

                ActiveJobs = jobs.Count(x =>
                    x.JobStatus == JobStatus.Active),

                PausedJobs = jobs.Count(x =>
                    x.JobStatus == JobStatus.Paused),

                ClosedJobs = jobs.Count(x =>
                    x.JobStatus == JobStatus.Closed),

                ArchivedJobs = jobs.Count(x =>
                    x.JobStatus == JobStatus.Archived),

                // =====================================================
                // Job Type
                // =====================================================

                NormalJobs = jobs.Count(x =>
                    x.JobType == "Regular Hiring"),

                ClassifiedJobs = jobs.Count(x =>
                    x.JobType == "Classified"),

                HotVacancyJobs = jobs.Count(x =>
                    x.JobType == "Hot Vacancy"),

                // =====================================================
                // Additional Analytics
                // =====================================================

                DraftJobs = jobs.Count(x =>
                    x.JobStatus == JobStatus.Draft),

                FeaturedJobs = jobs.Count(x =>
                    x.IsFeatured),

                UrgentHiringJobs = jobs.Count(x =>
                    x.IsUrgentHiring),

                TotalApplications = jobs.Sum(x =>
                    x.AppliedCount),

                TotalViews = jobs.Sum(x =>
                    x.ViewCount)
            };
        }

        public async Task<JobListResponseDto> GetJobsAsync(Guid employerId, JobListRequestDto request)
        {
            var query = _context.JobPostings
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId &&
                    !x.IsDeleted)
                .AsQueryable();

            // ==========================================
            // Search
            // ==========================================

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x =>
                    x.JobTitle.Contains(search) ||
                    x.TradeCategory.Contains(search) ||
                    (x.Role != null &&
                     x.Role.Contains(search)) ||
                    (x.Department != null &&
                     x.Department.Contains(search)));
            }

            // ==========================================
            // Status Filter
            // ==========================================

            if (!string.IsNullOrWhiteSpace(request.Status))
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

            // ==========================================
            // Job Type Filter
            // ==========================================

            if (!string.IsNullOrWhiteSpace(request.JobType))
            {
                query = query.Where(x => x.JobType == request.JobType);
            }
            // ==========================================
            // Total Count
            // ==========================================

            var totalRecords =
                await query.CountAsync();

            // ==========================================
            // Data
            // ==========================================

            var entities = await query
               .OrderByDescending(x => x.CreatedAt)
               .Skip((request.PageNumber - 1) * request.PageSize)
               .Take(request.PageSize)
               .ToListAsync();

            var jobs = entities.Select(x => new RecruiterJobListItemDto
            {
                JobId = x.JobId,

                JobTitle = x.JobTitle,
                TradeCategory = x.TradeCategory,
                Role = x.Role,
                Department = x.Department,

                JobType = x.JobType,

                EmploymentType = x.EmploymentType.ToString(),
                EmploymentMode = x.EmploymentMode.ToString(),
                JobStatus = x.JobStatus.ToString(),
                IndustryType = x.IndustryType.ToString(),

                IsActive = x.IsActive,
                IsFeatured = x.IsFeatured,
                IsUrgentHiring = x.IsUrgentHiring,

                AppliedCount = x.AppliedCount,
                ViewCount = x.ViewCount,
                Vacancies = x.Vacancies,

                ExperienceMinYears = x.ExperienceMinYears,
                ExperienceMaxYears = x.ExperienceMaxYears,

                SalaryMin = x.SalaryMin,
                SalaryMax = x.SalaryMax,
                SalaryCurrency = x.SalaryCurrency.ToString(),
                SalaryDisplayOption = x.SalaryDisplayOption.ToString(),

                ApplicationDeadline = x.ApplicationDeadline,
                CreatedAt = x.CreatedAt,
                PublishedAt = x.PublishedAt,

                Location = x.LocationType == LocationType.Onshore
                    ? string.Join(", ",
                        new[]
                        {
                x.OnshoreCity,
                x.OnshoreState
                        }
                        .Where(v => !string.IsNullOrWhiteSpace(v)))
                    : x.OffshoreRegion ?? string.Empty,

                LocationType = x.LocationType.ToString()

            }).ToList();

            return new JobListResponseDto
            {
                TotalRecords = totalRecords,

                PageNumber = request.PageNumber,

                PageSize = request.PageSize,

                Jobs = jobs
            };
        }

        public async Task<RecruiterJobDetailResponseDto?>
          GetJobByIdAsync(
              Guid employerId,
              Guid jobId)
        {
            var job = await _context.JobPostings
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
                // =====================================================
                // Basic
                // =====================================================

                JobId = job.JobId,

                JobTitle = job.JobTitle,

                JobDescription = job.JobDescription,

                TradeCategory = job.TradeCategory,

                Role = job.Role,

                Department = job.Department,

                IndustryType = job.IndustryType,
                // =====================================================
                // Job Type
                // =====================================================

                JobType = job.JobType,
                EmploymentType = job.EmploymentType,

                EmploymentMode = job.EmploymentMode,

                JobStatus = job.JobStatus.ToString(),

                // =====================================================
                // Salary
                // =====================================================

                SalaryMin = job.SalaryMin,

                SalaryMax = job.SalaryMax,

                SalaryCurrency = job.SalaryCurrency,

                SalaryDisplayOption = job.SalaryDisplayOption,

                // =====================================================
                // Vacancies & Experience
                // =====================================================

                Vacancies = job.Vacancies,

                ExperienceMinYears = job.ExperienceMinYears,

                ExperienceMaxYears = job.ExperienceMaxYears,

                DutyHoursPerDay = job.DutyHoursPerDay,

                PaidOvertime = job.PaidOvertime,

                // =====================================================
                // Eligibility
                // =====================================================

                EducationRequired = job.EducationRequired,

                GenderPreferred = job.GenderPreferred,

                AgeMin = job.AgeMin,

                AgeMax = job.AgeMax,

                DisabilityEligible = job.DisabilityEligible,

                PassportRequired = job.PassportRequired,

                PassportValidityMonths = job.PassportValidityMonths,

                // =====================================================
                // Skills
                // =====================================================

                KeySkills = job.KeySkills ?? new List<string>(),

                KeyResponsibilities =
                    job.KeyResponsibilities ?? new List<string>(),

                Benefits =
                    job.Benefits ?? new List<string>(),

                Tags =
                    job.Tags ?? new List<string>(),

                LanguageRequired = job.LanguageRequired,

                LicenceDocsRequired = job.LicenceDocsRequired,

                // =====================================================
                // Location
                // =====================================================

                LocationType = job.LocationType,

                WorkAddressLine = job.WorkAddressLine,

                OnshoreCity = job.OnshoreCity,

                OnshoreState = job.OnshoreState,

                OnshoreCountry = job.OnshoreCountry,

                OnshorePincode = job.OnshorePincode,

                OffshoreVesselName = job.OffshoreVesselName,

                OffshoreRegion = job.OffshoreRegion,

                OffshoreCountry = job.OffshoreCountry,

                IsInternational = job.IsInternational,

                // =====================================================
                // Publishing
                // =====================================================

                CompanyVisibility = job.CompanyVisibility,

                ApplicationDeadline = job.ApplicationDeadline,

                ScreeningQuestions =
                    job.ScreeningQuestions?
                        .Select(x => new ScreeningQuestion
                        {
                            QuestionText = x
                        })
                        .ToList()
                    ?? new List<ScreeningQuestion>(),

                //PublishingTags =
                //    job.PublishingTags ?? new List<string>(),

                // =====================================================
                // Analytics
                // =====================================================

                AppliedCount = job.AppliedCount,

                ViewCount = job.ViewCount,

                IsFeatured = job.IsFeatured,

                IsUrgentHiring = job.IsUrgentHiring,

                IsActive = job.IsActive,

                IsDeleted = job.IsDeleted,

                // =====================================================
                // Audit
                // =====================================================

                CurrentStep = job.CurrentStep,

                LastCompletedStep = job.LastCompletedStep,

                CreatedAt = job.CreatedAt,

                UpdatedAt = job.UpdatedAt,

                PublishedAt = job.PublishedAt
            };
        }

        public async Task<JobStatusUpdateResponseDto> PauseJobAsync(
        Guid employerId,
        Guid jobId,
        Guid actionUserId,
        bool isSubUser)
        {
            var permissionCheck = await _permissionService.CheckAsync(
                actionUserId, isSubUser, s => s.CanPostJobs);

            if (!permissionCheck.Allowed)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

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

        public async Task<JobStatusUpdateResponseDto> ResumeJobAsync(
        Guid employerId,
        Guid jobId,
        Guid actionUserId,
        bool isSubUser)
        {
            var permissionCheck = await _permissionService.CheckAsync(
                actionUserId, isSubUser, s => s.CanPostJobs);

            if (!permissionCheck.Allowed)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

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

        public async Task<JobStatusUpdateResponseDto> CloseJobAsync(
        Guid employerId,
        Guid jobId,
        Guid actionUserId,
        bool isSubUser)
        {
            var permissionCheck = await _permissionService.CheckAsync(
                actionUserId, isSubUser, s => s.CanPostJobs);

            if (!permissionCheck.Allowed)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

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

        public async Task<JobStatusUpdateResponseDto> ArchiveJobAsync(
        Guid employerId,
        Guid jobId,
        Guid actionUserId,
        bool isSubUser)
        {
            var permissionCheck = await _permissionService.CheckAsync(
                actionUserId, isSubUser, s => s.CanPostJobs);

            if (!permissionCheck.Allowed)
            {
                return new JobStatusUpdateResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

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