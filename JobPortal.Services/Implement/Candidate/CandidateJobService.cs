// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateJobService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateJobService : ICandidateJobService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateJobService> _logger;

    // Max items per page guard
    private const int MaxPageSize = 50;

    public CandidateJobService(AppDbContext context, ILogger<CandidateJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════
    // 1. JOB LIST — with search filters, sorting, pagination
    // ════════════════════════════════════════════════════════
    public async Task<CandidateJobListResponseDto> GetJobsAsync(
      CandidateJobSearchRequestDto request)
    {
        try
        {
            // Pagination
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            // Base Query
            var query = _context.JobPostings
                .Include(j => j.EmployerProfile)
                .Where(j =>
                    j.JobStatus == JobStatus.Active &&
                    !j.IsDeleted &&
                    j.IsActive)
                .AsQueryable();

            // Keyword Search
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim().ToLower();

                query = query.Where(j =>
                    j.JobTitle.ToLower().Contains(kw) ||
                    j.TradeCategory.ToLower().Contains(kw) ||
                    j.JobDescription.ToLower().Contains(kw) ||
                    (j.Role != null &&
                     j.Role.ToLower().Contains(kw)) ||

                    (j.KeySkills != null &&
                     j.KeySkills.Any(x =>
                        x.ToLower().Contains(kw))) ||

                    j.EmployerProfile.CompanyDisplayName
                        .ToLower()
                        .Contains(kw));
            }

            // Location
            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                var loc = request.Location.Trim().ToLower();

                query = query.Where(j =>
                    (j.OnshoreCity != null &&
                     j.OnshoreCity.ToLower().Contains(loc))

                    ||

                    (j.OnshoreState != null &&
                     j.OnshoreState.ToLower().Contains(loc))

                    ||

                    (j.OffshoreRegion != null &&
                     j.OffshoreRegion.ToLower().Contains(loc)));
            }

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                var state = request.State.Trim().ToLower();

                query = query.Where(j =>
                    j.OnshoreState != null &&
                    j.OnshoreState.ToLower().Contains(state));
            }

            if (!string.IsNullOrWhiteSpace(request.LocationType))
            {
                if (Enum.TryParse<LocationType>(
                    request.LocationType,
                    true,
                    out var locationType))
                {
                    query = query.Where(j =>
                        j.LocationType == locationType);
                }
            }

            // Trade Category
            if (!string.IsNullOrWhiteSpace(request.TradeCategory))
            {
                var trade = request.TradeCategory
                    .Trim()
                    .ToLower();

                query = query.Where(j =>
                    j.TradeCategory
                        .ToLower()
                        .Contains(trade));
            }

            // Role
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role
                    .Trim()
                    .ToLower();

                query = query.Where(j =>
                    j.Role != null &&
                    j.Role.ToLower().Contains(role));
            }

            // Employment Type
            if (!string.IsNullOrWhiteSpace(request.EmploymentType))
            {
                if (Enum.TryParse<EmploymentType>(
                    request.EmploymentType,
                    true,
                    out var employmentType))
                {
                    query = query.Where(j =>
                        j.EmploymentType == employmentType);
                }
            }

            // Experience
            if (request.ExperienceYearsMin.HasValue)
            {
                query = query.Where(j =>
                    j.ExperienceMaxYears >=
                    request.ExperienceYearsMin.Value);
            }

            if (request.ExperienceYearsMax.HasValue)
            {
                query = query.Where(j =>
                    j.ExperienceMinYears <=
                    request.ExperienceYearsMax.Value);
            }

            // Salary
            if (request.SalaryMin.HasValue)
            {
                query = query.Where(j =>
                    j.SalaryMax >=
                    request.SalaryMin.Value);
            }

            if (request.SalaryMax.HasValue)
            {
                query = query.Where(j =>
                    j.SalaryMin <=
                    request.SalaryMax.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SalaryCurrency))
            {
                if (Enum.TryParse<SalaryCurrency>(
                    request.SalaryCurrency,
                    true,
                    out var currency))
                {
                    query = query.Where(j =>
                        j.SalaryCurrency == currency);
                }
            }

            // Gender
            if (!string.IsNullOrWhiteSpace(request.Gender) &&
                request.Gender != "Any")
            {
                if (Enum.TryParse<GenderPreferred>(
                    request.Gender,
                    true,
                    out var gender))
                {
                    query = query.Where(j =>
                        j.GenderPreferred == gender ||
                        j.GenderPreferred ==
                        GenderPreferred.Any);
                }
            }

            // Education
            if (!string.IsNullOrWhiteSpace(
                request.EducationLevel))
            {
                query = query.Where(j =>
                    j.EducationRequired ==
                    request.EducationLevel);
            }

            // Disability
            if (request.DisabilityEligible.HasValue)
            {
                query = query.Where(j =>
                    j.DisabilityEligible ==
                    request.DisabilityEligible.Value);
            }

            // Passport
            if (request.PassportRequired.HasValue)
            {
                query = query.Where(j =>
                    j.PassportRequired ==
                    request.PassportRequired.Value);
            }

            // Posted Within
            if (request.PostedWithinDays.HasValue)
            {
                var cutoff =
                    DateTime.UtcNow.AddDays(
                        -request.PostedWithinDays.Value);

                query = query.Where(j =>
                    j.PublishedAt != null &&
                    j.PublishedAt >= cutoff);
            }

            // Deadline
            var today =
                DateOnly.FromDateTime(DateTime.UtcNow);

            query = query.Where(j =>
                j.ApplicationDeadline >= today);

            // Sort
            query = request.Sort switch
            {
                "oldest" =>
                    query.OrderBy(j => j.PublishedAt),

                "salary_high" =>
                    query.OrderByDescending(j => j.SalaryMax),

                "salary_low" =>
                    query.OrderBy(j => j.SalaryMin),

                _ =>
                    query.OrderByDescending(j => j.PublishedAt)
            };

            // Total Count
            var totalCount =
                await query.CountAsync();

            // Pagination
            var jobs = await query
                .Skip((request.Page - 1) *
                      request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var totalPages =
                (int)Math.Ceiling(
                    (double)totalCount /
                    request.PageSize);

            return new CandidateJobListResponseDto
            {
                Success = true,
                Message = $"{totalCount} job(s) found.",

                Jobs = jobs
                    .Select(MapToCard)
                    .ToList(),

                TotalCount = totalCount,

                Page = request.Page,

                PageSize = request.PageSize,

                TotalPages = totalPages,

                HasNextPage =
                    request.Page < totalPages,

                HasPreviousPage =
                    request.Page > 1,

                AppliedFilters = request
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CandidateJobService.GetJobsAsync error.");

            return new CandidateJobListResponseDto
            {
                Success = false,
                Message =
                    "An error occurred while fetching jobs."
            };
        }
    }

    // ════════════════════════════════════════════════════════
    // 2. JOB DETAIL — single job, full data
    // ════════════════════════════════════════════════════════
    public async Task<CandidateJobDetailResponseDto> GetJobDetailAsync(Guid jobId)
    {
        try
        {
            var job = await _context.JobPostings
                .Include(j => j.EmployerProfile)
                    .ThenInclude(e => e.Badges)
                .FirstOrDefaultAsync(j =>
                    j.JobId == jobId &&
                    j.JobStatus == JobStatus.Active &&
                    j.ApplicationDeadline >= DateOnly.FromDateTime(DateTime.UtcNow));

            if (job == null)
                return new CandidateJobDetailResponseDto
                {
                    Success = false,
                    Message = "Job not found or no longer active."
                };

            var employer = job.EmployerProfile;
            var isConfidential = job.CompanyVisibility == CompanyVisibility.ShowName;

            // ── Parse stored JSON fields ───────────────────────
            var skills = job.KeySkills;
            var screeningQuestions = job.ScreeningQuestions;
            var publishingTags = job.PublishingTags;
            var responsibilities = job.KeyResponsibilities ?? new List<string>();

            var benefits = job.Benefits ?? new List<string>();

            // ── Similar jobs (same trade, different job) ──────
            var similarJobs = await _context.JobPostings
                .Include(j => j.EmployerProfile)
                .Where(j =>
                    j.JobId != jobId &&
                    j.JobStatus == JobStatus.Active &&
                    j.TradeCategory == job.TradeCategory &&
                    j.ApplicationDeadline >= DateOnly.FromDateTime(DateTime.UtcNow))
                .OrderByDescending(j => j.PublishedAt)
                .Take(5)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            return new CandidateJobDetailResponseDto
            {
                Success = true,
                Message = "Job details retrieved successfully.",

                JobId = job.JobId,

                // Company
                CompanyName = isConfidential
          ? null
          : employer.CompanyDisplayName,

                CompanyLogoUrl = isConfidential
          ? null
          : employer.CompanyLogoUrl,

                IsConfidentialCompany = isConfidential,

                CompanyWebsite = isConfidential
          ? null
          : employer.WebsiteUrl,

                CompanyDescription = isConfidential
          ? null
          : employer.CompanyDescription,

                CompanyCity = isConfidential
          ? null
          : employer.City,

                CompanyState = isConfidential
          ? null
          : employer.State,

                CompanyAddress = isConfidential
          ? null
          : employer.AddressLine1,

                CompanyPhone = isConfidential
          ? null
          : employer.ContactPhone,

                CompanyEmail = isConfidential
          ? null
          : employer.ContactEmailPublic,

                CompanyIndustry =
          employer.IndustryType.ToString(),

                CompanySize =
          employer.CompanySize?.ToString(),

                HasPoeLicence =
          !string.IsNullOrWhiteSpace(
              employer.PoeLicenceUrl),

                HasRpslLicence =
          !string.IsNullOrWhiteSpace(
              employer.RpslLicenceUrl),

                // Job

                JobTitle = job.JobTitle,
                TradeCategory = job.TradeCategory,
                Role = job.Role,

                JobType =
          job.JobType.ToString(),

                EmploymentType =
          job.EmploymentType.ToString(),

                EmploymentMode =
          job.EmploymentMode.ToString(),

                Department =
          job.Department,

                JobDescription =
          job.JobDescription,

                // Location

                LocationType =
          job.LocationType.ToString(),

                WorkAddressLine =
          job.WorkAddressLine,

                City =
          job.OnshoreCity,

                State =
          job.OnshoreState,

                Country =
          job.OnshoreCountry,

                Pincode =
          job.OnshorePincode,

                OffshoreVesselName =
          job.OffshoreVesselName,

                OffshoreRegion =
          job.OffshoreRegion,

                IsInternational =
          job.IsInternational,

                // Salary

                SalaryDisplay =
          FormatSalary(job),

                SalaryMin =
          job.SalaryDisplayOption ==
          SalaryDisplayOption.Show_Min_Only
              ? null
              : job.SalaryMin,

                SalaryMax =
          job.SalaryDisplayOption ==
          SalaryDisplayOption.Show_Max_Only
              ? null
              : job.SalaryMax,

                SalaryCurrency =
          job.SalaryCurrency.ToString(),

                // Experience

                ExperienceMinYears =
          job.ExperienceMinYears,

                ExperienceMaxYears =
          job.ExperienceMaxYears,

                // Skills

                KeySkills = skills,

                KeyResponsibilities =
          responsibilities,

                Benefits =
          benefits,

                LicenceDocsRequired =
          job.LicenceDocsRequired,

                LanguageRequired =
          job.LanguageRequired,

                // Eligibility

                Vacancies =
          job.Vacancies,

                EducationRequired =
          job.EducationRequired,

                AgeMin =
          job.AgeMin,

                AgeMax =
          job.AgeMax,

                GenderPreferred =
          job.GenderPreferred.ToString(),

                DisabilityEligible =
          job.DisabilityEligible,

                PassportRequired =
          job.PassportRequired,

                PassportValidityMonths =
          job.PassportValidityMonths,

                // Employment Extras

                DutyHoursPerDay =
          job.DutyHoursPerDay,

                PaidOvertime =
          job.PaidOvertime,

                // Meta

                ApplicationDeadline =
          job.ApplicationDeadline,

                PublishedAt =
          job.PublishedAt,

                TimeAgo =
          GetTimeAgo(job.PublishedAt),

                AppliedCount =
          job.AppliedCount,

                ViewCount =
          job.ViewCount,

                IsFeatured =
          job.IsFeatured,

                IsUrgentHiring =
          job.IsUrgentHiring,

                IsDeadlineSoon =
          (job.ApplicationDeadline
              .ToDateTime(TimeOnly.MinValue)
              - DateTime.UtcNow)
              .TotalDays <= 7,

                Tags =
          BuildTags(job, publishingTags),

                ScreeningQuestions =
          screeningQuestions,

                SimilarJobs =
          similarJobs
              .Select(MapToCard)
              .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CandidateJobService.GetJobDetailAsync error. JobId={JobId}", jobId);
            return new CandidateJobDetailResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching job details."
            };
        }
    }

    // ════════════════════════════════════════════════════════
    // 3. SAVE / UNSAVE JOB
    // ════════════════════════════════════════════════════════
    public async Task<SaveJobResponseDto> ToggleSaveJobAsync(Guid jobId, Guid candidateId)
    {
        try
        {
            // Validate job exists and is active
            var jobExists = await _context.JobPostings
                .AnyAsync(j => j.JobId == jobId && j.JobStatus == JobStatus.Active);

            if (!jobExists)
                return new SaveJobResponseDto
                {
                    Success = false,
                    Message = "Job not found or no longer active.",
                    JobId = jobId,
                    IsSaved = false
                };

            var existing = await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.JobId == jobId && s.CandidateId == candidateId);

            if (existing != null)
            {
                // Already saved → unsave
                _context.SavedJobs.Remove(existing);
                await _context.SaveChangesAsync();
                return new SaveJobResponseDto
                {
                    Success = true,
                    Message = "Job removed from saved list.",
                    JobId = jobId,
                    IsSaved = false
                };
            }
            else
            {
                // Not saved → save
                var saved = new SavedJob
                {
                    SavedJobId = Guid.NewGuid(),
                    CandidateId = candidateId,
                    JobId = jobId,
                    SavedAt = DateTime.UtcNow
                };
                _context.SavedJobs.Add(saved);
                await _context.SaveChangesAsync();
                return new SaveJobResponseDto
                {
                    Success = true,
                    Message = "Job saved successfully.",
                    JobId = jobId,
                    IsSaved = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ToggleSaveJobAsync error. JobId={JobId} CandidateId={CandidateId}", jobId, candidateId);
            return new SaveJobResponseDto { Success = false, Message = "An error occurred." };
        }
    }

    // ════════════════════════════════════════════════════════
    // 4. FILTER OPTIONS — dynamic sidebar values
    // ════════════════════════════════════════════════════════
    public async Task<JobFilterOptionsResponseDto> GetFilterOptionsAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeJobs = await _context.JobPostings
                .AsNoTracking()
                .Where(j =>
                    j.JobStatus == JobStatus.Active &&
                    j.IsActive &&
                    !j.IsDeleted &&
                    j.ApplicationDeadline >= today)
                .ToListAsync();

            return new JobFilterOptionsResponseDto
            {
                Success = true,

                // Trade Categories
                TradeCategories = activeJobs
                    .Select(j => j.TradeCategory)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Roles
                Roles = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.Role))
                    .Select(j => j.Role!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Cities
                Cities = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.OnshoreCity))
                    .Select(j => j.OnshoreCity!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // States
                States = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.OnshoreState))
                    .Select(j => j.OnshoreState!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Countries
                Countries = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.OnshoreCountry))
                    .Select(j => j.OnshoreCountry!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Location Types
                LocationTypes = activeJobs
                    .Select(j => j.LocationType.ToString())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Employment Types
                EmploymentTypes = activeJobs
                    .Select(j => j.EmploymentType.ToString())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Employment Modes
                EmploymentModes = activeJobs
                    .Select(j => j.EmploymentMode.ToString())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Departments
                Departments = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.Department))
                    .Select(j => j.Department!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Education
                EducationLevels = activeJobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.EducationRequired))
                    .Select(j => j.EducationRequired!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Currency
                Currencies = activeJobs
                    .Select(j => j.SalaryCurrency.ToString())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Gender Options
                GenderOptions = Enum
                    .GetNames<GenderPreferred>()
                    .ToList(),

                // Skills
                Skills = activeJobs
                    .Where(j => j.KeySkills != null)
                    .SelectMany(j => j.KeySkills!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Benefits
                Benefits = activeJobs
                    .Where(j => j.Benefits != null)
                    .SelectMany(j => j.Benefits!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                // Salary
                MaxSalary = activeJobs
                    .Where(j =>
                        j.SalaryDisplayOption !=
                        SalaryDisplayOption.Show_Max_Only)
                    .Select(j => j.SalaryMax)
                    .DefaultIfEmpty(0)
                    .Max(),

                MinSalary = activeJobs
                    .Where(j =>
                        j.SalaryDisplayOption !=
                        SalaryDisplayOption.Show_Min_Only)
                    .Select(j => j.SalaryMin)
                    .DefaultIfEmpty(0)
                    .Min(),

                // Experience
                MaxExperienceYears = activeJobs
                    .Select(j => (int)j.ExperienceMaxYears)
                    .DefaultIfEmpty(0)
                    .Max(),

                MinExperienceYears = activeJobs
                    .Select(j => (int)j.ExperienceMinYears)
                    .DefaultIfEmpty(0)
                    .Min(),

                // Special Filters
                HasFeaturedJobs = activeJobs.Any(j => j.IsFeatured),

                HasUrgentHiringJobs = activeJobs.Any(j => j.IsUrgentHiring),

                HasInternationalJobs = activeJobs.Any(j => j.IsInternational),

                HasPassportRequiredJobs = activeJobs.Any(j => j.PassportRequired),

                // Stats
                TotalActiveJobs = activeJobs.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetFilterOptionsAsync error.");

            return new JobFilterOptionsResponseDto
            {
                Success = false
            };
        }
    }
    // ════════════════════════════════════════════════════════
    // ── Private helpers ──────────────────────────────────────
    // ════════════════════════════════════════════════════════

    /// <summary>Map a <see cref="JobPosting"/> to a compact card DTO.</summary>
    private static CandidateJobCardDto MapToCard(JobPosting job)
    {
        var isConfidential =
            job.CompanyVisibility ==
            CompanyVisibility.ShowName;

        var publishingTags =
            job.PublishingTags ?? new List<string>();

        var skills =
            job.KeySkills ?? new List<string>();

        var experienceDisplay =
            job.ExperienceMinYears == 0 &&
            job.ExperienceMaxYears == 0
                ? "Fresher"
                : job.ExperienceMaxYears > 0
                    ? $"{job.ExperienceMinYears}-{job.ExperienceMaxYears} Years"
                    : $"{job.ExperienceMinYears}+ Years";

        return new CandidateJobCardDto
        {
            JobId = job.JobId,

            // Company
            CompanyName = isConfidential
                ? null
                : job.EmployerProfile?.CompanyDisplayName,

            CompanyLogoUrl = isConfidential
                ? null
                : job.EmployerProfile?.CompanyLogoUrl,

            IsConfidentialCompany = isConfidential,

            // Job
            JobTitle = job.JobTitle,

            TradeCategory = job.TradeCategory,

            Role = job.Role,

            JobType =
                job.JobType.ToString(),

            EmploymentType =
                job.EmploymentType.ToString(),

            EmploymentMode =
                job.EmploymentMode.ToString(),

            Department =
                job.Department,

            // Location
            LocationType =
                job.LocationType.ToString(),

            City =
                job.OnshoreCity,

            State =
                job.OnshoreState,

            OffshoreRegion =
                job.OffshoreRegion,

            IsInternational =
                job.IsInternational,

            // Salary
            SalaryDisplay =
                FormatSalary(job),

            SalaryMin =
                job.SalaryDisplayOption ==
                SalaryDisplayOption.Show_Min_Only
                    ? null
                    : job.SalaryMin,

            SalaryMax =
                job.SalaryDisplayOption ==
                SalaryDisplayOption.Show_Max_Only
                    ? null
                    : job.SalaryMax,

            SalaryCurrency =
                job.SalaryCurrency.ToString(),

            // Experience
            ExperienceMinYears =
                job.ExperienceMinYears,

            ExperienceMaxYears =
                job.ExperienceMaxYears,

            ExperienceDisplay =
                experienceDisplay,

            // Eligibility
            EducationRequired =
                job.EducationRequired,

            GenderPreferred =
                job.GenderPreferred.ToString(),

            DisabilityEligible =
                job.DisabilityEligible,

            PassportRequired =
                job.PassportRequired,

            // Employment Details
            DutyHoursPerDay =
                job.DutyHoursPerDay,

            PaidOvertime =
                job.PaidOvertime,

            // Openings
            Vacancies =
                job.Vacancies,

            // Deadline
            ApplicationDeadline =
                job.ApplicationDeadline,

            IsDeadlineSoon =
                (job.ApplicationDeadline
                    .ToDateTime(TimeOnly.MinValue)
                    - DateTime.UtcNow)
                    .TotalDays <= 7,

            // Analytics
            AppliedCount =
                job.AppliedCount,

            ViewCount =
                job.ViewCount,

            IsFeatured =
                job.IsFeatured,

            IsUrgentHiring =
                job.IsUrgentHiring,

            // Tags
            Tags =
                BuildTags(job, publishingTags),

            KeySkills =
                skills.Take(5).ToList(),

            // Meta
            TimeAgo =
                GetTimeAgo(job.PublishedAt),

            PublishedAt =
                job.PublishedAt,

            // Description
            ShortDescription =
                TruncateDescription(
                    job.JobDescription,
                    160)
        };
    }
    // ── Salary formatting ─────────────────────────────────
    private static string? FormatSalary(JobPosting job)
    {
        if (job.SalaryDisplayOption ==
            SalaryDisplayOption.Show_Range)
        {
            return null;
        }

        var symbol = job.SalaryCurrency switch
        {
            SalaryCurrency.USD => "$",
            SalaryCurrency.AED => "AED ",
            SalaryCurrency.SAR => "SAR ",
            SalaryCurrency.EUR => "€",
            SalaryCurrency.GBP => "£",
            _ => "₹"
        };

        return job.SalaryDisplayOption switch
        {
            SalaryDisplayOption.Show_Min_Only =>
                $"{symbol}{job.SalaryMin:N0}+",

            SalaryDisplayOption.Show_Max_Only =>
                $"{symbol}{job.SalaryMax:N0}",

            SalaryDisplayOption.Show_Range =>
                $"{symbol}{job.SalaryMin:N0} - {symbol}{job.SalaryMax:N0} / month",

            _ =>
                $"{symbol}{job.SalaryMin:N0} - {symbol}{job.SalaryMax:N0} / month"
        };
    }

    // ── Time-ago string ───────────────────────────────────
    private static string GetTimeAgo(DateTime? publishedAt)
    {
        if (publishedAt == null) return "Recently";

        var diff = DateTime.UtcNow - publishedAt.Value;

        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} mins ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} weeks ago";
        return $"{(int)(diff.TotalDays / 30)} months ago";
    }

    // ── Tag builder ───────────────────────────────────────
    private static List<string> BuildTags(JobPosting job, List<string> publishingTags)
    {
        var tags = new List<string>(publishingTags);

        if (job.PassportRequired) tags.Add("Passport Required");
        if (job.IsInternational) tags.Add("International");
        if (job.DisabilityEligible) tags.Add("Disability Eligible");

        return tags.Distinct().ToList();
    }

    // ── JSON deserializers ────────────────────────────────
    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new List<string>(); }
    }

    private static List<CandidateScreeningQuestionDto> ParseScreeningQuestions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<CandidateScreeningQuestionDto>();

        try
        {
            var raw = JsonSerializer.Deserialize<List<RawScreeningQuestion>>(json);
            return raw?.Select(q => new CandidateScreeningQuestionDto
            {
                QuestionText = q.QuestionText,
                AnswerType = q.AnswerType,
                IsMandatory = q.IsMandatory
            }).ToList() ?? new();
        }
        catch { return new List<CandidateScreeningQuestionDto>(); }
    }

    // Matches the serialized format used by the recruiter service
    private record RawScreeningQuestion(
        string QuestionText,
        string AnswerType,
        bool IsMandatory);

    // ── Publishing tags helpers ───────────────────────────
    // JobType and EmploymentType are stored inside PublishingTags JSON
    private static readonly HashSet<string> KnownJobTypes =
        new() { "Normal_Job", "Hot_Vacancy", "Classified" };

    private static readonly HashSet<string> KnownEmploymentTypes =
        new() { "Permanent", "Contract", "Temporary", "Internship" };

    private static string GetJobTypeFromTags(List<string> tags) =>
        tags.FirstOrDefault(t => KnownJobTypes.Contains(t)) ?? "Normal_Job";

    private static string GetEmploymentTypeFromTags(List<string> tags) =>
        tags.FirstOrDefault(t => KnownEmploymentTypes.Contains(t)) ?? "Permanent";

    // ── Description truncation ────────────────────────────
    private static string TruncateDescription(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Strip simple HTML tags if present
        var plain = System.Text.RegularExpressions.Regex
            .Replace(text, "<[^>]+>", " ")
            .Replace("&nbsp;", " ")
            .Trim();

        return plain.Length <= maxLength
            ? plain
            : plain.Substring(0, maxLength).TrimEnd() + "…";
    }

    // ════════════════════════════════════════════════════════
    // 5. SAVED JOBS — candidate's bookmarked job list
    // ════════════════════════════════════════════════════════
    public async Task<SavedJobListResponseDto> GetSavedJobsAsync(Guid candidateId)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var savedJobs = await _context.SavedJobs
                .Include(s => s.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Include(s => s.JobPosting)
                    .ThenInclude(j => j.Applications)
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            // Fetch all application statuses for this candidate in one query
            var jobIds = savedJobs.Select(s => s.JobId).ToList();
            var applications = await _context.JobApplications
                .Where(a => a.CandidateId == candidateId && jobIds.Contains(a.JobId))
                .ToDictionaryAsync(a => a.JobId, a => a);

            var cards = savedJobs.Select(s =>
            {
                var job = s.JobPosting;

                var isConfidential =
                    job.CompanyVisibility ==
                    CompanyVisibility.ShowName;

                var publishingTags =
                    job.PublishingTags ?? new List<string>();

                var isExpired =
                    job.ApplicationDeadline < today;

                var isActive =
                    job.JobStatus == JobStatus.Active &&
                    !isExpired;

                applications.TryGetValue(
                    job.JobId,
                    out var application);

                var locationDisplay =
                    job.LocationType == LocationType.Offshore
                        ? $"Offshore - {job.OffshoreRegion ?? "Region TBD"}"
                        : string.Join(", ",
                            new[]
                            {
                    job.OnshoreCity,
                    job.OnshoreState
                            }
                            .Where(x => !string.IsNullOrWhiteSpace(x)));

                string experienceDisplay;

                if (job.ExperienceMinYears == 0 &&
                    job.ExperienceMaxYears == 0)
                {
                    experienceDisplay = "Fresher";
                }
                else if (job.ExperienceMaxYears == 0)
                {
                    experienceDisplay =
                        $"{job.ExperienceMinYears}+ Years";
                }
                else
                {
                    experienceDisplay =
                        $"{job.ExperienceMinYears}-{job.ExperienceMaxYears} Years";
                }

                return new SavedJobCardDto
                {
                    SavedJobId = s.SavedJobId,
                    SavedAt = s.SavedAt,

                    JobId = job.JobId,

                    // Company
                    CompanyName = isConfidential
                        ? null
                        : job.EmployerProfile?.CompanyDisplayName,

                    CompanyLogoUrl = isConfidential
                        ? null
                        : job.EmployerProfile?.CompanyLogoUrl,

                    IsConfidentialCompany = isConfidential,

                    // Job
                    JobTitle = job.JobTitle,

                    TradeCategory = job.TradeCategory,

                    City = job.OnshoreCity,

                    State = job.OnshoreState,

                    LocationDisplay = locationDisplay,

                    EmploymentType =
                        job.EmploymentType.ToString(),

                    EmploymentMode =
                        job.EmploymentMode.ToString(),

                    JobType =
                        job.JobType.ToString(),

                    Department =
                        job.Department,

                    ExperienceDisplay =
                        experienceDisplay,

                    // Salary
                    SalaryDisplay =
                        FormatSalary(job),

                    SalaryMin =
                        job.SalaryDisplayOption ==
                        SalaryDisplayOption.Show_Min_Only
                            ? null
                            : job.SalaryMin,

                    SalaryMax =
                        job.SalaryDisplayOption ==
                        SalaryDisplayOption.Show_Max_Only
                            ? null
                            : job.SalaryMax,

                    SalaryCurrency =
                        job.SalaryCurrency.ToString(),

                    // Meta
                    ApplicationDeadline =
                        job.ApplicationDeadline,

                    IsDeadlineSoon =
                        (job.ApplicationDeadline
                            .ToDateTime(TimeOnly.MinValue)
                            - DateTime.UtcNow)
                            .TotalDays <= 7,

                    IsExpired = isExpired,

                    TimeAgo =
                        GetTimeAgo(job.PublishedAt),

                    AppliedCount =
                        job.AppliedCount,

                    ViewCount =
                        job.ViewCount,

                    IsFeatured =
                        job.IsFeatured,

                    IsUrgentHiring =
                        job.IsUrgentHiring,

                    // Skills
                    Tags =
                        BuildTags(job, publishingTags),

                    KeySkills =
                        (job.KeySkills ?? new List<string>())
                            .Take(3)
                            .ToList(),

                    // Application
                    ApplicationId =
                        application?.ApplicationId,

                    ApplicationStatus =
                        application?.ApplicationStatus
                            .ToString(),

                    StatusNote =
                        BuildStatusNote(
                            application?.ApplicationStatus
                                .ToString(),
                            job.JobTitle)
                };
            }).ToList();

            return new SavedJobListResponseDto
            {
                Success = true,
                Message = $"{cards.Count} saved job(s) found.",
                SavedJobs = cards,
                TotalCount = cards.Count,
                ActiveCount = cards.Count(c => !c.IsExpired),
                ExpiredCount = cards.Count(c => c.IsExpired),
                AppliedCount = cards.Count(c => c.ApplicationStatus != null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSavedJobsAsync error. CandidateId={CandidateId}", candidateId);
            return new SavedJobListResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching saved jobs."
            };
        }
    }

    // ════════════════════════════════════════════════════════
    // 6. APPLY NOW — submit application with screening answers
    // ════════════════════════════════════════════════════════
    public async Task<ApplyJobResponseDto> ApplyJobAsync(
     Guid jobId,
     Guid candidateId,
     ApplyJobRequestDto request)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // ─────────────────────────────────────────────
            // Load Job
            // ─────────────────────────────────────────────

            var job = await _context.JobPostings
                .Include(j => j.EmployerProfile)
                .FirstOrDefaultAsync(j =>
                    j.JobId == jobId &&
                    j.JobStatus == JobStatus.Active &&
                    j.IsActive &&
                    !j.IsDeleted &&
                    j.ApplicationDeadline >= today);

            if (job == null)
            {
                return ApplyFail(
                    "This job is no longer accepting applications.");
            }

            // ─────────────────────────────────────────────
            // Load Candidate
            // ─────────────────────────────────────────────

            var candidate = await _context.CandidateProfiles
                .Include(c => c.Cvs)
                .FirstOrDefaultAsync(c =>
                    c.CandidateId == candidateId &&
                    c.ProfileStatus == "Active");

            if (candidate == null)
            {
                return ApplyFail(
                    "Candidate profile not found.");
            }

            // ─────────────────────────────────────────────
            // Prevent Duplicate Application
            // ─────────────────────────────────────────────

            var alreadyApplied =
                await _context.JobApplications
                    .AnyAsync(a =>
                        a.JobId == jobId &&
                        a.CandidateId == candidateId);

            if (alreadyApplied)
            {
                return ApplyFail(
                    "You have already applied to this job.");
            }

            // ─────────────────────────────────────────────
            // Passport Validation
            // ─────────────────────────────────────────────

            if (job.PassportRequired &&
                request.PassportGatePassed == false)
            {
                return ApplyFail(
                    "A valid passport is required to apply for this job.");
            }

            // ─────────────────────────────────────────────
            // Screening Questions Validation
            // ─────────────────────────────────────────────

            if (job.ScreeningQuestions != null &&
                job.ScreeningQuestions.Any())
            {
                foreach (var question in job.ScreeningQuestions)
                {
                    var answered =
                        request.ScreeningAnswers.Any(x =>
                            x.QuestionText.Equals(
                                question,
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(x.Answer));

                    if (!answered)
                    {
                        return ApplyFail(
                            $"Mandatory question not answered: \"{question}\"");
                    }
                }
            }

            // ─────────────────────────────────────────────
            // Serialize Answers
            // ─────────────────────────────────────────────

            var answersJson =
                request.ScreeningAnswers != null &&
                request.ScreeningAnswers.Any()
                    ? JsonSerializer.Serialize(
                        request.ScreeningAnswers)
                    : null;

            // ─────────────────────────────────────────────
            // Create Application
            // ─────────────────────────────────────────────

            var application =
                new JobApplication
                {
                    ApplicationId = Guid.NewGuid(),

                    JobId = job.JobId,

                    CandidateId = candidateId,

                    EmployerId = job.EmployerId,

                    AppliedAt = DateTime.UtcNow,

                    ApplicationStatus =
                        ApplicationStatus.Applied,

                    StatusUpdatedAt =
                        DateTime.UtcNow,

                    PassportGatePassed =
                        request.PassportGatePassed ?? true,

                    WithdrawalAllowed = true,

                    RejectionAutoNotify = true
                };

            _context.JobApplications.Add(application);

            // ─────────────────────────────────────────────
            // Update Job Analytics
            // ─────────────────────────────────────────────

            job.AppliedCount++;

            // ─────────────────────────────────────────────
            // Update Candidate
            // ─────────────────────────────────────────────

            candidate.LastAppliedAt =
                DateTime.UtcNow;

            // ─────────────────────────────────────────────
            // Save
            // ─────────────────────────────────────────────

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Application submitted. ApplicationId:{ApplicationId}, JobId:{JobId}, CandidateId:{CandidateId}",
                application.ApplicationId,
                job.JobId,
                candidateId);

            // ─────────────────────────────────────────────
            // Response
            // ─────────────────────────────────────────────

            return new ApplyJobResponseDto
            {
                Success = true,

                Message =
                    "Application submitted successfully.",

                ApplicationId =
                    application.ApplicationId,

                JobId =
                    job.JobId,

                JobTitle =
                    job.JobTitle,

                CompanyName =
                    job.CompanyVisibility ==
                    CompanyVisibility.ShowName
                        ? null
                        : job.EmployerProfile.CompanyDisplayName,

                ApplicationStatus =
                    application.ApplicationStatus.ToString(),

                AppliedAt =
                    application.AppliedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "ApplyJobAsync error. JobId={JobId}, CandidateId={CandidateId}",
                jobId,
                candidateId);

            return ApplyFail(
                "An unexpected error occurred. Please try again.");
        }
    }

    // ════════════════════════════════════════════════════════
    // 7. MY APPLICATIONS — candidate's application history
    // ════════════════════════════════════════════════════════
    public async Task<MyApplicationsResponseDto> GetMyApplicationsAsync(Guid candidateId)
    {
        try
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var cards = apps.Select(a =>
            {
                var job = a.JobPosting;
                var isConfidential = job.CompanyVisibility == CompanyVisibility.ShowName;
                var publishingTags =job.PublishingTags;

                return new MyApplicationCardDto
                {
                    ApplicationId = a.ApplicationId,
                    JobId = job.JobId,
                    JobTitle = job.JobTitle,
                    TradeCategory = job.TradeCategory,
                    CompanyName = isConfidential ? null : job.EmployerProfile?.CompanyDisplayName,
                    CompanyLogoUrl = isConfidential ? null : job.EmployerProfile?.CompanyLogoUrl,
                    IsConfidentialCompany = isConfidential,
                    City = job.OnshoreCity,
                    State = job.OnshoreState,
                    EmploymentType = GetEmploymentTypeFromTags(publishingTags),
                    SalaryDisplay = FormatSalary(job),
                    ApplicationStatus = a.ApplicationStatus.ToString(),
                    AppliedAt = a.AppliedAt,
                    AppliedTimeAgo = GetTimeAgo(a.AppliedAt),
                    StatusUpdatedAt = a.StatusUpdatedAt,
                    WithdrawalAllowed = a.WithdrawalAllowed &&
                                       a.ApplicationStatus.ToString() != "Hired" &&
                                       a.ApplicationStatus.ToString() != "Rejected"
                };
            }).ToList();

            return new MyApplicationsResponseDto
            {
                Success = true,
                Message = $"{cards.Count} application(s) found.",
                Applications = cards,
                TotalCount = cards.Count,
                ActiveCount = cards.Count(c =>
                    c.ApplicationStatus != "Rejected" &&
                    c.ApplicationStatus != "Withdrawn" &&
                    c.ApplicationStatus != "Hired"),
                RejectedCount = cards.Count(c => c.ApplicationStatus == "Rejected"),
                HiredCount = cards.Count(c => c.ApplicationStatus == "Hired")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyApplicationsAsync error. CandidateId={CandidateId}", candidateId);
            return new MyApplicationsResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching applications."
            };
        }
    }

    // ════════════════════════════════════════════════════════
    // 8. WITHDRAW APPLICATION
    // ════════════════════════════════════════════════════════
    public async Task<WithdrawApplicationResponseDto> WithdrawApplicationAsync(
        Guid applicationId, Guid candidateId)
    {
        try
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.CandidateId == candidateId);

            if (application == null)
                return new WithdrawApplicationResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };

            if (!application.WithdrawalAllowed)
                return new WithdrawApplicationResponseDto
                {
                    Success = false,
                    Message = "This application cannot be withdrawn."
                };

            if (application.ApplicationStatus.ToString() == "Hired" ||
                application.ApplicationStatus.ToString() == "Rejected")
                return new WithdrawApplicationResponseDto
                {
                    Success = false,
                    Message = $"Cannot withdraw an application with status '{application.ApplicationStatus}'."
                };

            application.ApplicationStatus = ApplicationStatus.Withdrawn;
            application.StatusUpdatedAt = DateTime.UtcNow;
            application.WithdrawalAllowed = false;

            // Decrement the job's applied count
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.JobId == application.JobId);
            if (job != null && job.AppliedCount > 0)
                job.AppliedCount -= 1;

            await _context.SaveChangesAsync();

            return new WithdrawApplicationResponseDto
            {
                Success = true,
                Message = "Application withdrawn successfully.",
                ApplicationId = applicationId,
                ApplicationStatus = "Withdrawn"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "WithdrawApplicationAsync error. ApplicationId={AppId}", applicationId);
            return new WithdrawApplicationResponseDto
            {
                Success = false,
                Message = "An error occurred while withdrawing the application."
            };
        }
    }

    // ── Apply helper ──────────────────────────────────────────
    private static ApplyJobResponseDto ApplyFail(string message) =>
        new() { Success = false, Message = message };

    // ── Status note builder (shown on Saved Jobs card) ────────
    private static string? BuildStatusNote(string? status, string jobTitle) =>
        status switch
        {
            "Applied" => $"Your application for {jobTitle} is under review.",
            "Viewed" => $"Employer viewed your application for {jobTitle}.",
            "Shortlisted" => $"You have been shortlisted for {jobTitle}.",
            "Interview" => $"Interview scheduled for {jobTitle}. Check your email.",
            "Hired" => $"Congratulations! You were hired for {jobTitle}.",
            "Rejected" => $"Application for {jobTitle} was not selected.",
            "Withdrawn" => $"You withdrew your application for {jobTitle}.",
            _ => null
        };
}