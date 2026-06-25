// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateJobService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate;
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


    public async Task<List<CandidateJobListItemDto>> GetAllJobsAsync()
    {
        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        var jobs =
            await _context.JobPostings
                .AsNoTracking()
                .Include(x => x.EmployerProfile)
                    .ThenInclude(x => x.Badges)
                .Where(x =>
                    x.JobStatus == JobStatus.Active &&
                      x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today)
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ToListAsync();

        return jobs.Select(job =>
        {
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

            string jobLocation =
                job.LocationType == LocationType.Offshore
                    ? job.OffshoreRegion ?? "Offshore"
                    : string.Join(", ",
                        new[]
                        {
                        job.OnshoreCity,
                        job.OnshoreState
                        }
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x)));

            string companyLocation =
                string.Join(", ",
                    new[]
                    {
                    job.EmployerProfile?.City,
                    job.EmployerProfile?.State
                    }
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x)));

            return new CandidateJobListItemDto
            {
                JobId = job.JobId,
                EmployerId = job.EmployerId,
                CompanyLogoUrl =
                    job.EmployerProfile?.CompanyLogoUrl,

                CompanyName =
                    job.CompanyVisibility ==
                    CompanyVisibility.ShowName
                        ? job.EmployerProfile?.CompanyDisplayName
                        : "Confidential Company",

                JobTitle = job.JobTitle,

                TradeCategory = job.TradeCategory,

                Department = job.Department,

                EmploymentType =
                    job.EmploymentType.ToString(),

                EmploymentMode =
                    job.EmploymentMode.ToString(),

                JobType =
                    job.JobType.ToString(),

                JobLocation = jobLocation,

                CompanyLocation = companyLocation,

                SalaryDisplay =
                    FormatSalary(job) ?? "Confidential",

                ExperienceDisplay =
                    experienceDisplay,

                Vacancies =
                    job.Vacancies,

                ApplicationsCount =
                    job.AppliedCount,

                ViewCount =
                    job.ViewCount,

                PostedOn =
                    job.PublishedAt,

                TimeAgo =
                    GetTimeAgo(job.PublishedAt),

                Description =
                    job.JobDescription.Length > 150
                        ? job.JobDescription.Substring(0, 150) + "..."
                        : job.JobDescription,

                Skills =
                    job.KeySkills?.Take(5).ToList()
                    ?? new List<string>(),

                IsFeatured =
                    job.IsFeatured,

                IsUrgentHiring =
                    job.IsUrgentHiring,

                PassportRequired =
                    job.PassportRequired,

                IsInternational =
                    job.IsInternational,

                AiMatchPercentage = null,

                CompanyVerified =
                    job.EmployerProfile?.Badges?.Any() == true,

                ApplicationDeadline =
                    job.ApplicationDeadline
            };
        }).ToList();
    }

    public async Task<CandidateJobDetailsDto?> GetJobDetailsAsync(Guid jobId)
    {
        var job = await _context.JobPostings
            .AsNoTracking()
            .Include(x => x.EmployerProfile)
                .ThenInclude(x => x.Badges)
            .FirstOrDefaultAsync(x =>
                x.JobId == jobId &&
                x.JobStatus == JobStatus.Active &&
                x.IsActive &&
                !x.IsDeleted);

        if (job == null)
            return null;

        var employer = job.EmployerProfile;

        var companyLocation = string.Join(", ",
            new[]
            {
            employer?.City,
            employer?.State,
            employer?.Country
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        var jobLocation =
            job.LocationType == LocationType.Offshore
                ? string.Join(", ",
                    new[]
                    {
                    job.OffshoreRegion,
                    job.OffshoreCountry
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                : string.Join(", ",
                    new[]
                    {
                    job.OnshoreCity,
                    job.OnshoreState,
                    job.OnshoreCountry
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

        return new CandidateJobDetailsDto
        {
            JobId = job.JobId,

            CompanyLogoUrl = employer?.CompanyLogoUrl,

            CompanyName =
                job.CompanyVisibility == CompanyVisibility.ShowName
                    ? employer?.CompanyDisplayName
                    : "Confidential Company",

            CompanyLocation = companyLocation,

            CompanyLocationMapLink =
                !string.IsNullOrWhiteSpace(employer?.AddressLine1)
                    ? $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(employer.AddressLine1)}"
                    : null,

            VerificationBadges =
                employer?.Badges?
                    .Where(x => x.BadgeStatus == BadgeStatus.Approved)
                    .Select(x => x.BadgeType.ToString())
                    .ToList()
                    ?? new List<string>(),

            AiMatchPercentage = null,

            JobTitle = job.JobTitle,

            TradeCategory = job.TradeCategory,

            Department = job.Department,

            EmploymentType = job.EmploymentType.ToString(),

            EmploymentMode = job.EmploymentMode.ToString(),

            JobType = job.JobType.ToString(),

            JobLocation = jobLocation,

            LocationType = job.LocationType.ToString(),

            SalaryRange = FormatSalary(job) ?? "Confidential",

            ApplicationCount = job.AppliedCount,

            OpeningCount = job.Vacancies,

            PostedOn = job.PublishedAt,

            ApplicationDeadline = job.ApplicationDeadline,

            ExperienceMinYears = job.ExperienceMinYears,

            ExperienceMaxYears = job.ExperienceMaxYears,

            EducationRequired = job.EducationRequired,

            AgeMin = job.AgeMin,

            AgeMax = job.AgeMax,

            GenderPreferred = job.GenderPreferred.ToString(),

            DisabilityFriendly = job.DisabilityEligible,

            IsInternational = job.IsInternational,

            PassportRequired = job.PassportRequired,

            DutyHoursPerDay = job.DutyHoursPerDay,

            PaidOvertime = job.PaidOvertime,

            LanguagePreferred = job.LanguageRequired,

            RequiredLicencesCertificates =
                job.LicenceDocsRequired,

            JobDescription = job.JobDescription,

            KeyResponsibilities =
                job.KeyResponsibilities ??
                new List<string>(),

            ProfessionalSkills =
                job.KeySkills ??
                new List<string>(),

            PerksAndBenefits =
                job.Benefits ??
                new List<string>()
        };
    }

    public async Task<CandidateCompanyDetailResponseDto?> GetCompanyDetailAsync(
    Guid employerId)
    {
        var company = await _context.EmployerProfiles
            .AsNoTracking()
            .Include(x => x.Badges)
            .FirstOrDefaultAsync(x => x.EmployerId == employerId);

        if (company == null)
            return null;

        var activeJobs = await _context.JobPostings
            .CountAsync(x =>
                x.EmployerId == employerId &&
                x.JobStatus == JobStatus.Active &&
                x.IsActive &&
                !x.IsDeleted);

        var totalJobs = await _context.JobPostings
            .CountAsync(x =>
                x.EmployerId == employerId);

        return new CandidateCompanyDetailResponseDto
        {
            EmployerId = company.EmployerId,

            CompanyName = company.CompanyDisplayName,
            TradeName = company.TradeName,

            CompanyLogoUrl = company.CompanyLogoUrl,
            CoverImageUrl = company.CoverImageUrl,

            CompanyDescription = company.CompanyDescription,

            IndustryType = company.IndustryType.ToString(),
            BusinessType = company.BusinessType.ToString(),
            CompanySize = company.CompanySize?.ToString(),

            TotalEmployees = company.TotalEmployees,
            YearEstablished = company.YearEstablished,

            AddressLine1 = company.AddressLine1,
            AddressLine2 = company.AddressLine2,
            City = company.City,
            State = company.State,
            Country = company.Country,
            Pincode = company.Pincode,

            OfficeAddress = company.OfficeAddress,

            FullLocation = string.Join(", ",
                new[]
                {
                company.City,
                company.State,
                company.Country
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))),

            IsVerified = company.Badges.Any(),

            HasPoeLicence =
                !string.IsNullOrWhiteSpace(company.PoeLicenceUrl),

            HasRpslLicence =
                !string.IsNullOrWhiteSpace(company.RpslLicenceUrl),

            VerificationBadges = company.Badges
                .Select(x => x.BadgeType.ToString())
                .ToList(),

            OpenPositionsCount = activeJobs,

            TotalJobsPosted = totalJobs,

            WebsiteUrl = company.WebsiteUrl,
            LinkedInUrl = company.LinkedInUrl,
            FacebookUrl = company.FacebookUrl,
            InstagramUrl = company.InstagramUrl,

            ProfileCompletionScore =
                company.ProfileCompletionScore
        };
    }
    // ════════════════════════════════════════════════════════
    // 1. JOB LIST — with search filters, sorting, pagination
    // ════════════════════════════════════════════════════════
    public async Task<CandidateJobListResponseDto> GetJobsAsync(
      CandidateJobSearchRequestDto request)
    {
        try
        {
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = _context.JobPostings
                .AsNoTracking()
                .Include(x => x.EmployerProfile)
                    .ThenInclude(x => x.Badges)
                .Where(x =>
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today);

            //------------------------------------------------
            // Keyword
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();

                query = query.Where(j =>

                    j.JobTitle.ToLower().Contains(keyword)

                    ||

                    j.TradeCategory.ToLower().Contains(keyword)

                    ||

                    (j.Role != null &&
                     j.Role.ToLower().Contains(keyword))

                    ||

                    (j.Department != null &&
                     j.Department.ToLower().Contains(keyword))

                    ||

                    j.JobDescription.ToLower().Contains(keyword)

                    ||

                    (j.SearchKeywords != null &&
                     j.SearchKeywords.ToLower().Contains(keyword))

                    ||

                    (j.KeySkills != null &&
                     j.KeySkills.Any(s =>
                        s.ToLower().Contains(keyword)))

                    ||

                    j.EmployerProfile.CompanyDisplayName
                        .ToLower()
                        .Contains(keyword));
            }

            //------------------------------------------------
            // Trade Category
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.TradeCategory))
            {
                var trade = request.TradeCategory.Trim().ToLower();

                query = query.Where(j =>
                    j.TradeCategory.ToLower().Contains(trade));
            }

            //------------------------------------------------
            // Role
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role.Trim().ToLower();

                query = query.Where(j =>
                    j.Role != null &&
                    j.Role.ToLower().Contains(role));
            }

            //------------------------------------------------
            // Location
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                var location = request.Location.Trim().ToLower();

                query = query.Where(j =>

                    (j.OnshoreCity != null &&
                     j.OnshoreCity.ToLower().Contains(location))

                    ||

                    (j.OnshoreState != null &&
                     j.OnshoreState.ToLower().Contains(location))

                    ||

                    (j.OnshoreCountry != null &&
                     j.OnshoreCountry.ToLower().Contains(location))

                    ||

                    (j.OffshoreRegion != null &&
                     j.OffshoreRegion.ToLower().Contains(location))

                    ||

                    (j.OffshoreCountry != null &&
                     j.OffshoreCountry.ToLower().Contains(location)));
            }

            //------------------------------------------------
            // State
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                var state = request.State.Trim().ToLower();

                query = query.Where(j =>
                    j.OnshoreState != null &&
                    j.OnshoreState.ToLower().Contains(state));
            }

            //------------------------------------------------
            // Location Type
            //------------------------------------------------

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

            //------------------------------------------------
            // Employment Type
            //------------------------------------------------

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

            //------------------------------------------------
            // Experience
            //------------------------------------------------

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

            //------------------------------------------------
            // Salary
            //------------------------------------------------

            if (request.SalaryMin.HasValue)
            {
                query = query.Where(j =>
                    j.SalaryMax >= request.SalaryMin.Value);
            }

            if (request.SalaryMax.HasValue)
            {
                query = query.Where(j =>
                    j.SalaryMin <= request.SalaryMax.Value);
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

            //------------------------------------------------
            // Gender
            //------------------------------------------------

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
                        j.GenderPreferred == GenderPreferred.Any);
                }
            }

            //------------------------------------------------
            // Education
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.EducationLevel))
            {
                query = query.Where(j =>
                    j.EducationRequired == request.EducationLevel);
            }

            //------------------------------------------------
            // Disability
            //------------------------------------------------

            if (request.DisabilityEligible.HasValue)
            {
                query = query.Where(j =>
                    j.DisabilityEligible ==
                    request.DisabilityEligible.Value);
            }

            //------------------------------------------------
            // Passport
            //------------------------------------------------

            if (request.PassportRequired.HasValue)
            {
                query = query.Where(j =>
                    j.PassportRequired ==
                    request.PassportRequired.Value);
            }

            //------------------------------------------------
            // Posted Within
            //------------------------------------------------

            if (request.PostedWithinDays.HasValue)
            {
                var cutoff =
                    DateTime.UtcNow.AddDays(
                        -request.PostedWithinDays.Value);

                query = query.Where(j =>
                    j.PublishedAt != null &&
                    j.PublishedAt >= cutoff);
            }

            //------------------------------------------------
            // Sorting
            //------------------------------------------------

            query = request.Sort switch
            {
                "oldest" =>
                    query.OrderBy(j => j.PublishedAt),

                "salary_high" =>
                    query.OrderByDescending(j => j.SalaryMax),

                "salary_low" =>
                    query.OrderBy(j => j.SalaryMin),

                _ =>
                    query.OrderByDescending(j => j.IsFeatured)
                         .ThenByDescending(j => j.PublishedAt)
            };

            //------------------------------------------------
            // Count
            //------------------------------------------------

            var totalCount =
                await query.CountAsync();

            //------------------------------------------------
            // Pagination
            //------------------------------------------------

            var jobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var totalPages =
                (int)Math.Ceiling(
                    (double)totalCount /
                    request.PageSize);

            //------------------------------------------------
            // Map Jobs
            //------------------------------------------------

            var jobCards = jobs
                .Select(job => MapToCard(job))
                .ToList();

            //------------------------------------------------
            // Response
            //------------------------------------------------

            return new CandidateJobListResponseDto
            {
                Success = true,

                Message =
                    $"{totalCount} job(s) found.",

                Jobs =
                    jobCards,

                TotalCount =
                    totalCount,

                Page =
                    request.Page,

                PageSize =
                    request.PageSize,

                TotalPages =
                    totalPages,

                HasNextPage =
                    request.Page < totalPages,

                HasPreviousPage =
                    request.Page > 1,

                AppliedFilters =
                    request
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
                    ex.InnerException?.Message ??
                    ex.Message
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
            _logger.LogError(
                ex,
                "ToggleSaveJobAsync error. JobId={JobId} CandidateId={CandidateId}",
                jobId,
                candidateId);

            return new SaveJobResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message,
                JobId = jobId,
                CandidateId= candidateId,
                IsSaved = false
            };
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
    private CandidateJobListItemDto MapToCard(JobPosting job)
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

        return new CandidateJobListItemDto
        {
            JobId = job.JobId,

            EmployerId = job.EmployerId,

            CompanyLogoUrl =
          job.CompanyVisibility == CompanyVisibility.ShowName
              ? job.EmployerProfile?.CompanyLogoUrl
              : null,

            CompanyName =
          job.CompanyVisibility == CompanyVisibility.ShowName
              ? job.EmployerProfile?.CompanyDisplayName
              : "Confidential Company",

            JobTitle = job.JobTitle,

            TradeCategory = job.TradeCategory,

            Department = job.Department,

            EmploymentType = job.EmploymentType.ToString(),

            EmploymentMode = job.EmploymentMode.ToString(),

            JobType = job.JobType.ToString(),

            JobLocation =
          job.LocationType == LocationType.Offshore
              ? string.Join(", ",
                  new[]
                  {
                    job.OffshoreRegion,
                    job.OffshoreCountry
                  }.Where(x => !string.IsNullOrWhiteSpace(x)))
              : string.Join(", ",
                  new[]
                  {
                    job.OnshoreCity,
                    job.OnshoreState
                  }.Where(x => !string.IsNullOrWhiteSpace(x))),

            CompanyLocation =
          string.Join(", ",
              new[]
              {
                job.EmployerProfile?.City,
                job.EmployerProfile?.State
              }.Where(x => !string.IsNullOrWhiteSpace(x))),

            SalaryDisplay = FormatSalary(job),

            ExperienceDisplay =
          job.ExperienceMinYears == 0 &&
          job.ExperienceMaxYears == 0
              ? "Fresher"
              : job.ExperienceMaxYears > 0
                  ? $"{job.ExperienceMinYears}-{job.ExperienceMaxYears} Years"
                  : $"{job.ExperienceMinYears}+ Years",

            Vacancies = job.Vacancies,

            ApplicationsCount = job.AppliedCount,

            ViewCount = job.ViewCount,

            PostedOn = job.PublishedAt,

            TimeAgo = GetTimeAgo(job.PublishedAt),

            Description =
          TruncateDescription(job.JobDescription, 160),

            Skills =
          job.KeySkills?
              .Take(5)
              .ToList()
          ?? new List<string>(),

            IsFeatured = job.IsFeatured,

            IsUrgentHiring = job.IsUrgentHiring,

            PassportRequired = job.PassportRequired,

            IsInternational = job.IsInternational,

            CompanyVerified =
          job.EmployerProfile?.Badges?.Any() == true,

            ApplicationDeadline = job.ApplicationDeadline
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
                //AnswerType = q.AnswerType,
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

    public async Task<ApplyJobDetailsResponseDto> GetApplyJobDetailsAsync(
    Guid jobId,
    Guid candidateId)
    {
        try
        {
            var job = await _context.JobPostings
                .Include(x => x.EmployerProfile)
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted);

            if (job == null)
            {
                return new ApplyJobDetailsResponseDto
                {
                    Success = false,
                    Message = "Job not found."
                };
            }

            var candidate = await _context.CandidateProfiles
                .Include(x => x.Cvs)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId);

            if (candidate == null)
            {
                return new ApplyJobDetailsResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };
            }

            //----------------------------------------------------
            // Languages
            //----------------------------------------------------

            var languages =
                string.IsNullOrWhiteSpace(job.LanguageRequired)
                    ? new List<string>()
                    : job.LanguageRequired
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();

            //----------------------------------------------------
            // Certificates
            //----------------------------------------------------

            var certificates =
                string.IsNullOrWhiteSpace(job.LicenceDocsRequired)
                    ? new List<string>()
                    : job.LicenceDocsRequired
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();

            //----------------------------------------------------
            // Location
            //----------------------------------------------------

            string location =
                job.LocationType == LocationType.Offshore
                    ? string.Join(", ",
                        new[]
                        {
                        job.OffshoreRegion,
                        job.OffshoreCountry
                        }
                        .Where(x => !string.IsNullOrWhiteSpace(x)))

                    : string.Join(", ",
                        new[]
                        {
                        job.OnshoreCity,
                        job.OnshoreState
                        }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

            //----------------------------------------------------
            // Response
            //----------------------------------------------------

            return new ApplyJobDetailsResponseDto
            {
                Success = true,

                Message = "Apply details fetched successfully.",

                JobId = job.JobId,

                CompanyName =
                    job.CompanyVisibility == CompanyVisibility.ShowName
                        ? job.EmployerProfile?.CompanyDisplayName
                        : "Confidential Company",

                CompanyLogoUrl =
                    job.CompanyVisibility == CompanyVisibility.ShowName
                        ? job.EmployerProfile?.CompanyLogoUrl
                        : null,

                IsConfidentialCompany =
                    job.CompanyVisibility != CompanyVisibility.ShowName,

                JobTitle =
                    job.JobTitle,

                EmploymentType =
                    job.EmploymentType.ToString(),

                Department =
                    job.Department,

                Location =
                    location,

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

                LanguagesRequired =
                    languages,

                CertificatesRequired =
                    certificates,

                ScreeningQuestions =
                    job.ScreeningQuestions ?? new List<string>(),

                HasUploadedCv =
                    candidate.Cvs.Any(),

                CandidateName =
                    candidate.FullName,

                CandidatePhotoUrl =
                    candidate.ProfilePhotoUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetApplyJobDetailsAsync failed. JobId:{JobId}, CandidateId:{CandidateId}",
                jobId,
                candidateId);

            return new ApplyJobDetailsResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApplyJobResponseDto> ApplyJobAsync(
        Guid jobId,
        Guid candidateId,
        ApplyJobRequestDto request)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            //-----------------------------------------------------
            // Load Job
            //-----------------------------------------------------

            var job = await _context.JobPostings
                .Include(x => x.EmployerProfile)
                .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today);

            if (job == null)
                return ApplyFail("This job is no longer accepting applications.");

            //-----------------------------------------------------
            // Vacancy
            //-----------------------------------------------------

            if (job.Vacancies <= 0)
                return ApplyFail("This position has already been filled.");

            //-----------------------------------------------------
            // Candidate
            //-----------------------------------------------------

            var candidate = await _context.CandidateProfiles
                .Include(x => x.Cvs)
                .FirstOrDefaultAsync(x =>
                    x.CandidateId == candidateId &&
                    x.ProfileStatus == "Active");

            if (candidate == null)
                return ApplyFail("Candidate profile not found.");

            //-----------------------------------------------------
            // CV
            //-----------------------------------------------------

            if (!candidate.Cvs.Any())
                return ApplyFail("Please upload your CV before applying.");

            //-----------------------------------------------------
            // Duplicate Application
            //-----------------------------------------------------

            bool alreadyApplied = await _context.JobApplications
                .AnyAsync(x =>
                    x.JobId == jobId &&
                    x.CandidateId == candidateId);

            if (alreadyApplied)
                return ApplyFail("You have already applied to this job.");

            //-----------------------------------------------------
            // Screening Answers (Optional)
            //-----------------------------------------------------

            List<string>? screeningAnswers = null;

            if (request.ScreeningAnswers != null &&
                request.ScreeningAnswers.Any())
            {
                screeningAnswers = request.ScreeningAnswers
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.QuestionText) &&
                        !string.IsNullOrWhiteSpace(x.Answer))
                    .Select(x => $"{x.QuestionText}: {x.Answer}")
                    .ToList();
            }

            //-----------------------------------------------------
            // Create Application
            //-----------------------------------------------------

            var application = new JobApplication
            {
                ApplicationId = Guid.NewGuid(),

                JobId = job.JobId,

                EmployerId = job.EmployerId,

                CandidateId = candidateId,

                AppliedAt = DateTime.UtcNow,

                StatusUpdatedAt = DateTime.UtcNow,

                ApplicationStatus = ApplicationStatus.Applied,

                // Optional values

                PassportGatePassed = request.PassportGatePassed ?? false,

                MotivationMessage = request.MotivationMessage,

                ScreeningAnswers = screeningAnswers,

                WithdrawalAllowed = true,

                RejectionAutoNotify = true
            };

            _context.JobApplications.Add(application);

            //-----------------------------------------------------
            // Analytics
            //-----------------------------------------------------

            job.AppliedCount++;

            candidate.LastAppliedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Candidate {CandidateId} applied for Job {JobId}",
                candidateId,
                jobId);

            //-----------------------------------------------------
            // Response
            //-----------------------------------------------------

            return new ApplyJobResponseDto
            {
                Success = true,

                Message = "Application submitted successfully.",

                ApplicationId = application.ApplicationId,

                JobId = job.JobId,

                JobTitle = job.JobTitle,

                CompanyName =
                    job.CompanyVisibility == CompanyVisibility.ShowName
                        ? job.EmployerProfile?.CompanyDisplayName
                        : "Confidential Company",

                ApplicationStatus =
                    application.ApplicationStatus.ToString(),

                AppliedAt = application.AppliedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "ApplyJobAsync failed. JobId:{JobId}, CandidateId:{CandidateId}",
                jobId,
                candidateId);

            return ApplyFail(
                ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ════════════════════════════════════════════════════════
    // 7. MY APPLICATIONS — candidate's application history
    // ════════════════════════════════════════════════════════
    public async Task<MyApplicationsResponseDto> GetMyApplicationsAsync(Guid candidateId)
    {
        try
        {
            var applications = await _context.JobApplications
                .AsNoTracking()
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.EmployerProfile)
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var cards = applications.Select(a =>
            {
                var job = a.JobPosting;

                bool showCompany =
                    job.CompanyVisibility == CompanyVisibility.ShowName;

                string jobLocation =
                    job.LocationType == LocationType.Offshore
                        ? string.Join(", ",
                            new[]
                            {
                            job.OffshoreRegion,
                            job.OffshoreCountry
                            }
                            .Where(x => !string.IsNullOrWhiteSpace(x)))
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

                return new MyApplicationCardDto
                {
                    ApplicationId = a.ApplicationId,

                    JobId = job.JobId,

                    JobTitle = job.JobTitle,

                    TradeCategory = job.TradeCategory,

                    Department = job.Department,

                    EmploymentType = job.EmploymentType.ToString(),

                    EmploymentMode = job.EmploymentMode.ToString(),

                    JobType = job.JobType.ToString(),

                    ExperienceDisplay = experienceDisplay,

                    JobLocation = jobLocation,

                    CompanyName =
                        showCompany
                            ? job.EmployerProfile?.CompanyDisplayName
                            : "Confidential Company",

                    CompanyLogoUrl =
                        showCompany
                            ? job.EmployerProfile?.CompanyLogoUrl
                            : null,

                    IsConfidentialCompany = !showCompany,

                    SalaryDisplay = FormatSalary(job),

                    Tags = job.Tags ?? new List<string>(),

                    ApplicationsCount = job.AppliedCount,

                    ViewCount = job.ViewCount,

                    IsFeatured = job.IsFeatured,

                    IsUrgentHiring = job.IsUrgentHiring,

                    ApplicationStatus =
                        a.ApplicationStatus.ToString(),

                    AppliedAt = a.AppliedAt,

                    AppliedTimeAgo =
                        GetTimeAgo(a.AppliedAt),

                    StatusUpdatedAt =
                        a.StatusUpdatedAt,

                    WithdrawalAllowed =
                        a.WithdrawalAllowed &&
                        a.ApplicationStatus != ApplicationStatus.Hired &&
                        a.ApplicationStatus != ApplicationStatus.Rejected &&
                        a.ApplicationStatus != ApplicationStatus.Withdrawn
                };
            }).ToList();

            return new MyApplicationsResponseDto
            {
                Success = true,

                Message = $"{cards.Count} application(s) found.",

                Applications = cards,

                TotalCount = cards.Count,

                ActiveCount = cards.Count(x =>
                    x.ApplicationStatus != ApplicationStatus.Rejected.ToString() &&
                    x.ApplicationStatus != ApplicationStatus.Withdrawn.ToString() &&
                    x.ApplicationStatus != ApplicationStatus.Hired.ToString()),

                RejectedCount = cards.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Rejected.ToString()),

                HiredCount = cards.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Hired.ToString())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetMyApplicationsAsync error. CandidateId={CandidateId}",
                candidateId);

            return new MyApplicationsResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message,
                Applications = new List<MyApplicationCardDto>()
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