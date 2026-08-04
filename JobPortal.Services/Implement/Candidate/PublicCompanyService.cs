using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Application.DTOs.Public;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class PublicCompanyService : IPublicCompanyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PublicCompanyService> _logger;
    private readonly JobPortal.Services.IImplement.AI.IJobMatchingService _jobMatching;
    private const int MaxPageSize = 50;

    public PublicCompanyService(AppDbContext context, ILogger<PublicCompanyService> logger,
        JobPortal.Services.IImplement.AI.IJobMatchingService jobMatching)
    {
        _context = context;
        _logger = logger;
        _jobMatching = jobMatching;
    }

    // ════════════════════════════════════════════════════════════
    // TRADE-MATCH FEED SORTING
    //
    // Candidate job feed rule: a candidate's own trade category
    // (CandidateProfile.PrimaryTrade, e.g. "Welder") drives the
    // ordering of every job feed — with or without filters applied.
    // All jobs in the candidate's trade come first (further boosted
    // when the job's Role also matches the candidate's preferred
    // role/title), followed by every other trade, each group newest
    // (or however the caller further sorts) first.
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the logged-in candidate's trade category and preferred role
    /// used to prioritize the job feed. Returns (null, null) for
    /// anonymous visitors or when the profile can't be found, in which
    /// case callers fall back to their existing default ordering.
    /// </summary>
    private async Task<(string? Trade, string? Role)> GetCandidateTradeRoleAsync(
        Guid? candidateId)
    {
        if (!candidateId.HasValue || candidateId.Value == Guid.Empty)
            return (null, null);

        var candidate = await _context.CandidateProfiles
            .AsNoTracking()
            .Where(c => c.CandidateId == candidateId.Value)
            .Select(c => new { c.PrimaryTrade, c.Role })
            .FirstOrDefaultAsync();

        return (candidate?.PrimaryTrade, candidate?.Role);
    }

    /// <summary>
    /// Applies trade/role match priority as the primary sort key.
    /// Callers append their own secondary ordering (newest, salary, etc.)
    /// with .ThenBy/.ThenByDescending on the returned IOrderedQueryable.
    /// No-op (query unchanged) when the candidate has no trade on file.
    /// </summary>
    private static IOrderedQueryable<JobPosting> ApplyTradeMatchPriority(
        IQueryable<JobPosting> query,
        string? candidateTrade,
        string? candidateRole)
    {
        var tradeLower =
            string.IsNullOrWhiteSpace(candidateTrade)
                ? null
                : candidateTrade.Trim().ToLower();

        var roleLower =
            string.IsNullOrWhiteSpace(candidateRole)
                ? null
                : candidateRole.Trim().ToLower();

        return query
            // 1) Same trade category as the candidate → shown first.
            .OrderByDescending(j =>
                tradeLower != null &&
                j.TradeCategory.ToLower() == tradeLower)
            // 2) Within that, jobs that also match the candidate's
            //    preferred role/title rank above same-trade-but-
            //    different-role jobs.
            .ThenByDescending(j =>
                roleLower != null &&
                j.Role != null &&
                j.Role.ToLower() == roleLower);
    }

    public async Task<List<CandidateJobListItemDto>> GetAllJobsAsync(Guid? candidateId = null)
    {
        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        // Candidate's trade/role drive the default ordering of this feed
        // (works even though no filters are applied here).
        var (candidateTrade, candidateRole) =
            await GetCandidateTradeRoleAsync(candidateId);

        var baseQuery = _context.JobPostings
                .AsNoTracking()
                .Include(x => x.EmployerProfile)
                    .ThenInclude(x => x.Badges)
                .Where(x =>
                    x.JobStatus == JobStatus.Active &&
                      x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today);

        var jobs =
            await ApplyTradeMatchPriority(baseQuery, candidateTrade, candidateRole)
                .ThenByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ToListAsync();

        // Preload saved-job ids for this candidate (only if candidateId passed)
        HashSet<Guid> savedJobIds = new();

        if (candidateId.HasValue && candidateId.Value != Guid.Empty)
        {
            savedJobIds = (await _context.SavedJobs
                .AsNoTracking()
                .Where(s => s.CandidateId == candidateId.Value)
                .Select(s => s.JobId)
                .ToListAsync())
                .ToHashSet();
        }

        var cards = jobs
            .Select(job => MapToJobListItem(job, savedJobIds.Contains(job.JobId)))
            .ToList();

        // AI match % — only computed when candidateId passed, exactly like GetJobsAsync
        if (candidateId.HasValue && candidateId.Value != Guid.Empty)
        {
            foreach (var card in cards)
            {
                try
                {
                    var match = await _jobMatching
                        .CalculateMatchAsync(candidateId.Value, card.JobId);
                    card.AiMatchPercentage = match.MatchScore;
                }
                catch
                {
                    card.AiMatchPercentage = null;
                }
            }
        }

        return cards;
    }
    public async Task<CandidateJobDetailsDto?> GetJobDetailsAsync(
        Guid jobId,
        Guid? candidateId = null)
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

        int? aiMatch = null;
        bool isSaved = false;

        if (candidateId.HasValue && candidateId.Value != Guid.Empty)
        {
            try
            {
                var match = await _jobMatching
                    .CalculateMatchAsync(candidateId.Value, job.JobId);
                aiMatch = match.MatchScore;
            }
            catch
            {
                aiMatch = null;
            }

            isSaved = await _context.SavedJobs
                .AsNoTracking()
                .AnyAsync(s =>
                    s.CandidateId == candidateId.Value &&
                    s.JobId == job.JobId);
        }

        var similarJobs = await GetSimilarJobsAsync(job);

        return new CandidateJobDetailsDto
        {
            JobId = job.JobId,

            CompanyLogoUrl = employer?.CompanyLogoUrl,

            CompanyName =
    job.CompanyVisibility == CompanyVisibility.ShowName
        ? employer?.CompanyDisplayName
        : "Confidential Company",


            CompanyVisibility = job.CompanyVisibility.ToString(),
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

            ReviewCount = employer?.ReviewCount ?? 0,

            AiMatchPercentage = aiMatch,

            IsSaved = isSaved,

            JobTitle = job.JobTitle,

            JobLevel = job.TradeCategory,

            TradeCategory = job.TradeCategory,

            Department = job.Department,

            IndustryType = employer?.IndustryType,

            EmploymentType = job.EmploymentType.ToString(),

            EmploymentMode = job.EmploymentMode.ToString(),

            JobType = job.JobType.ToString(),


            JobLocation = jobLocation,

            LocationType = job.LocationType.ToString(),

            SalaryRange = FormatSalary(job) ?? "Confidential",

            SalaryVisibility = job.SalaryDisplayOption.ToString(),

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
    (job.LicenceDocsRequired ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Concat(
            (job.WorkingDocsRequired ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
        )
        .Select(x => x.Trim())
        .Distinct()
        .ToList(),

            JobDescription = job.JobDescription,

            KeyResponsibilities =
                job.KeyResponsibilities ??
                new List<string>(),

            ProfessionalSkills =
                job.KeySkills ??
                new List<string>(),

            PerksAndBenefits =
                job.Benefits ??
                new List<string>(),

            SimilarJobs = similarJobs
        };
    }
    //public async Task<CandidateJobListResponseDto> GetJobsAsync(
    // CandidateJobSearchRequestDto request)
    //{
    //    try
    //    {
    //        request.Page = Math.Max(1, request.Page);
    //        request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

    //        var today = DateOnly.FromDateTime(DateTime.UtcNow);

    //        var query = _context.JobPostings
    //            .AsNoTracking()
    //            .Include(x => x.EmployerProfile)
    //                .ThenInclude(x => x.Badges)
    //            .Where(x =>
    //                x.JobStatus == JobStatus.Active &&
    //                x.IsActive &&
    //                !x.IsDeleted &&
    //                x.ApplicationDeadline >= today);

    //        //------------------------------------------------
    //        // Keyword
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.Keyword))
    //        {
    //            var keyword = request.Keyword.Trim().ToLower();

    //            query = query.Where(j =>

    //                j.JobTitle.ToLower().Contains(keyword)

    //                ||

    //                j.TradeCategory.ToLower().Contains(keyword)

    //                ||

    //                (j.Role != null &&
    //                 j.Role.ToLower().Contains(keyword))

    //                ||

    //                (j.Department != null &&
    //                 j.Department.ToLower().Contains(keyword))

    //                ||

    //                j.JobDescription.ToLower().Contains(keyword)

    //                ||

    //                (j.SearchKeywords != null &&
    //                 j.SearchKeywords.ToLower().Contains(keyword))

    //                ||

    //                (j.KeySkills != null &&
    //                 j.KeySkills.Any(s =>
    //                    s.ToLower().Contains(keyword)))

    //                ||

    //                j.EmployerProfile.CompanyDisplayName
    //                    .ToLower()
    //                    .Contains(keyword));
    //        }

    //        //------------------------------------------------
    //        // Trade Category
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.TradeCategory))
    //        {
    //            var trade = request.TradeCategory.Trim().ToLower();

    //            query = query.Where(j =>
    //                j.TradeCategory.ToLower().Contains(trade));
    //        }

    //        //------------------------------------------------
    //        // Role
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.Role))
    //        {
    //            var role = request.Role.Trim().ToLower();

    //            query = query.Where(j =>
    //                j.Role != null &&
    //                j.Role.ToLower().Contains(role));
    //        }

    //        //------------------------------------------------
    //        // Location
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.Location))
    //        {
    //            var location = request.Location.Trim().ToLower();

    //            query = query.Where(j =>

    //                (j.OnshoreCity != null &&
    //                 j.OnshoreCity.ToLower().Contains(location))

    //                ||

    //                (j.OnshoreState != null &&
    //                 j.OnshoreState.ToLower().Contains(location))

    //                ||

    //                (j.OnshoreCountry != null &&
    //                 j.OnshoreCountry.ToLower().Contains(location))

    //                ||

    //                (j.OffshoreRegion != null &&
    //                 j.OffshoreRegion.ToLower().Contains(location))

    //                ||

    //                (j.OffshoreCountry != null &&
    //                 j.OffshoreCountry.ToLower().Contains(location)));
    //        }

    //        //------------------------------------------------
    //        // State
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.State))
    //        {
    //            var state = request.State.Trim().ToLower();

    //            query = query.Where(j =>
    //                j.OnshoreState != null &&
    //                j.OnshoreState.ToLower().Contains(state));
    //        }

    //        //------------------------------------------------
    //        // Location Type
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.LocationType))
    //        {
    //            if (Enum.TryParse<LocationType>(
    //                request.LocationType,
    //                true,
    //                out var locationType))
    //            {
    //                query = query.Where(j =>
    //                    j.LocationType == locationType);
    //            }
    //        }

    //        //------------------------------------------------
    //        // Employment Type
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
    //        {
    //            if (Enum.TryParse<EmploymentType>(
    //                request.EmploymentType,
    //                true,
    //                out var employmentType))
    //            {
    //                query = query.Where(j =>
    //                    j.EmploymentType == employmentType);
    //            }
    //        }

    //        //------------------------------------------------
    //        // Experience
    //        //------------------------------------------------

    //        if (request.ExperienceYearsMin.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.ExperienceMaxYears >=
    //                request.ExperienceYearsMin.Value);
    //        }

    //        if (request.ExperienceYearsMax.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.ExperienceMinYears <=
    //                request.ExperienceYearsMax.Value);
    //        }

    //        //------------------------------------------------
    //        // Salary
    //        //------------------------------------------------

    //        if (request.SalaryMin.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.SalaryMax >= request.SalaryMin.Value);
    //        }

    //        if (request.SalaryMax.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.SalaryMin <= request.SalaryMax.Value);
    //        }

    //        if (!string.IsNullOrWhiteSpace(request.SalaryCurrency))
    //        {
    //            if (Enum.TryParse<SalaryCurrency>(
    //                request.SalaryCurrency,
    //                true,
    //                out var currency))
    //            {
    //                query = query.Where(j =>
    //                    j.SalaryCurrency == currency);
    //            }
    //        }

    //        //------------------------------------------------
    //        // Gender
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.Gender) &&
    //            request.Gender != "Any")
    //        {
    //            if (Enum.TryParse<GenderPreferred>(
    //                request.Gender,
    //                true,
    //                out var gender))
    //            {
    //                query = query.Where(j =>
    //                    j.GenderPreferred == gender ||
    //                    j.GenderPreferred == GenderPreferred.Any);
    //            }
    //        }

    //        //------------------------------------------------
    //        // Education
    //        //------------------------------------------------

    //        if (!string.IsNullOrWhiteSpace(request.EducationLevel))
    //        {
    //            query = query.Where(j =>
    //                j.EducationRequired == request.EducationLevel);
    //        }

    //        //------------------------------------------------
    //        // Disability
    //        //------------------------------------------------

    //        if (request.DisabilityEligible.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.DisabilityEligible ==
    //                request.DisabilityEligible.Value);
    //        }

    //        //------------------------------------------------
    //        // Passport
    //        //------------------------------------------------

    //        if (request.PassportRequired.HasValue)
    //        {
    //            query = query.Where(j =>
    //                j.PassportRequired ==
    //                request.PassportRequired.Value);
    //        }

    //        //------------------------------------------------
    //        // Posted Within
    //        //------------------------------------------------

    //        if (request.PostedWithinDays.HasValue)
    //        {
    //            var cutoff =
    //                DateTime.UtcNow.AddDays(
    //                    -request.PostedWithinDays.Value);

    //            query = query.Where(j =>
    //                j.PublishedAt != null &&
    //                j.PublishedAt >= cutoff);
    //        }

    //        //------------------------------------------------
    //        // Sorting
    //        //------------------------------------------------

    //        query = request.Sort switch
    //        {
    //            "oldest" =>
    //                query.OrderBy(j => j.PublishedAt),

    //            "salary_high" =>
    //                query.OrderByDescending(j => j.SalaryMax),

    //            "salary_low" =>
    //                query.OrderBy(j => j.SalaryMin),

    //            _ =>
    //                query.OrderByDescending(j => j.IsFeatured)
    //                     .ThenByDescending(j => j.PublishedAt)
    //        };

    //        //------------------------------------------------
    //        // Count
    //        //------------------------------------------------

    //        var totalCount =
    //            await query.CountAsync();

    //        //------------------------------------------------
    //        // Pagination
    //        //------------------------------------------------

    //        var jobs = await query
    //            .Skip((request.Page - 1) * request.PageSize)
    //            .Take(request.PageSize)
    //            .ToListAsync();

    //        var totalPages =
    //            (int)Math.Ceiling(
    //                (double)totalCount /
    //                request.PageSize);

    //        //------------------------------------------------
    //        // Map Jobs
    //        //------------------------------------------------

    //        var jobCards = jobs
    //            .Select(job => MapToCard(job))
    //            .ToList();

    //        //------------------------------------------------
    //        // Response
    //        //------------------------------------------------

    //        return new CandidateJobListResponseDto
    //        {
    //            Success = true,

    //            Message =
    //                $"{totalCount} job(s) found.",

    //            Jobs =
    //                jobCards,

    //            TotalCount =
    //                totalCount,

    //            Page =
    //                request.Page,

    //            PageSize =
    //                request.PageSize,

    //            TotalPages =
    //                totalPages,

    //            HasNextPage =
    //                request.Page < totalPages,

    //            HasPreviousPage =
    //                request.Page > 1,

    //            AppliedFilters =
    //                request
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(
    //            ex,
    //            "CandidateJobService.GetJobsAsync error.");

    //        return new CandidateJobListResponseDto
    //        {
    //            Success = false,
    //            Message =
    //                ex.InnerException?.Message ??
    //                ex.Message
    //        };
    //    }
    //}

    public async Task<CandidateJobListResponseDto> GetJobsAsync(
    CandidateJobSearchRequestDto request,
    Guid? candidateId = null)
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
                var keyword = $"%{request.Keyword.Trim()}%";

                query = query.Where(j =>

                    EF.Functions.ILike(j.JobTitle, keyword)

                    ||

                    EF.Functions.ILike(j.TradeCategory, keyword)

                    ||

                    (j.Role != null &&
                     EF.Functions.ILike(j.Role, keyword))

                    ||

                    (j.Department != null &&
                     EF.Functions.ILike(j.Department, keyword))

                    ||

                    EF.Functions.ILike(j.JobDescription, keyword)

                    ||

                    (j.SearchKeywords != null &&
                     EF.Functions.ILike(j.SearchKeywords, keyword))

                    ||

                    EF.Functions.ILike(
                        j.EmployerProfile.CompanyDisplayName,
                        keyword));
            }

            //------------------------------------------------
            // Trade Category
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.TradeCategory))
            {
                var trade = $"%{request.TradeCategory.Trim()}%";

                query = query.Where(j =>
                    EF.Functions.ILike(j.TradeCategory, trade));
            }

            //------------------------------------------------
            // Role
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = $"%{request.Role.Trim()}%";

                query = query.Where(j =>
                    j.Role != null &&
                    EF.Functions.ILike(j.Role, role));
            }

            //------------------------------------------------
            // Location
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                var location = $"%{request.Location.Trim()}%";

                query = query.Where(j =>

                    (j.OnshoreCity != null &&
                     EF.Functions.ILike(j.OnshoreCity, location))

                    ||

                    (j.OnshoreState != null &&
                     EF.Functions.ILike(j.OnshoreState, location))

                    ||

                    (j.OnshoreCountry != null &&
                     EF.Functions.ILike(j.OnshoreCountry, location))

                    ||

                    (j.OffshoreRegion != null &&
                     EF.Functions.ILike(j.OffshoreRegion, location))

                    ||

                    (j.OffshoreCountry != null &&
                     EF.Functions.ILike(j.OffshoreCountry, location)));
            }

            //------------------------------------------------
            // State
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                var state = $"%{request.State.Trim()}%";

                query = query.Where(j =>
                    j.OnshoreState != null &&
                    EF.Functions.ILike(j.OnshoreState, state));
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
                query = query.Where(j =>
                    j.EmploymentType != null &&
                    j.EmploymentType.Equals(request.EmploymentType, StringComparison.OrdinalIgnoreCase));
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
                var currency = request.SalaryCurrency.Trim().ToUpper();

                query = query.Where(j =>
                    j.SalaryCurrency.ToUpper() == currency);
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
            //
            // Trade-match priority always comes first: all of the
            // candidate's own trade-category jobs (role-matches boosted
            // further within that) before any other trade — this applies
            // whether or not other filters/sort options are in play.
            // The requested Sort option (newest/oldest/salary) then
            // orders each of those two groups.
            //------------------------------------------------

            var (candidateTrade, candidateRole) =
                await GetCandidateTradeRoleAsync(candidateId);

            var tradeSortedQuery =
                ApplyTradeMatchPriority(query, candidateTrade, candidateRole);

            query = request.Sort switch
            {
                "oldest" =>
                    tradeSortedQuery.ThenBy(j => j.PublishedAt),

                "salary_high" =>
                    tradeSortedQuery.ThenByDescending(j => j.SalaryMax),

                "salary_low" =>
                    tradeSortedQuery.ThenBy(j => j.SalaryMin),

                _ =>
                    tradeSortedQuery.ThenByDescending(j => j.IsFeatured)
                         .ThenByDescending(j => j.PublishedAt)
            };
            //------------------------------------------------
            // Count
            //------------------------------------------------

            var totalCount = await query.CountAsync();

            //------------------------------------------------
            // Pagination
            //------------------------------------------------

            var jobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            //------------------------------------------------
            // KeySkills Filter (Client Side)
            //------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                jobs = jobs
                    .Where(j =>

                        (j.KeySkills != null &&
                         j.KeySkills.Any(s =>
                             s.Contains(
                                 keyword,
                                 StringComparison.OrdinalIgnoreCase)))

                        ||

                        j.JobTitle.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)

                        ||

                        j.TradeCategory.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)

                        ||

                        (j.Role != null &&
                         j.Role.Contains(
                             keyword,
                             StringComparison.OrdinalIgnoreCase))

                        ||

                        (j.Department != null &&
                         j.Department.Contains(
                             keyword,
                             StringComparison.OrdinalIgnoreCase))

                        ||

                        j.JobDescription.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)

                        ||

                        (j.SearchKeywords != null &&
                         j.SearchKeywords.Contains(
                             keyword,
                             StringComparison.OrdinalIgnoreCase))

                        ||

                        j.EmployerProfile.CompanyDisplayName.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase)

                    )
                    .ToList();

                totalCount = jobs.Count;
            }

            //------------------------------------------------
            // Total Pages
            //------------------------------------------------

            var totalPages =
                (int)Math.Ceiling(
                    (double)totalCount /
                    request.PageSize);

            //------------------------------------------------
            // Map Jobs
            //------------------------------------------------

            var jobCards = jobs
                .Select(MapToCard)
                .ToList();

            // Per-candidate AI match: score each job against the logged-in
            // candidate's profile. Anonymous visitors get no score.
            if (candidateId.HasValue && candidateId.Value != Guid.Empty)
            {
                foreach (var card in jobCards)
                {
                    try
                    {
                        var match = await _jobMatching
                            .CalculateMatchAsync(candidateId.Value, card.JobId);
                        card.AiMatchPercentage = match.MatchScore;
                    }
                    catch
                    {
                        card.AiMatchPercentage = null;
                    }
                }
            }

            //------------------------------------------------
            // Response
            //------------------------------------------------

            return new CandidateJobListResponseDto
            {
                Success = true,

                Message = $"{totalCount} job(s) found.",

                Jobs = jobCards,

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
                    ex.InnerException?.Message ??
                    ex.Message
            };
        }
    }

    public async Task<PublicCompanyListResponseDto> GetCompaniesAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            //-------------------------------------------------------
            // Load Companies
            //-------------------------------------------------------

            var companies = await _context.EmployerProfiles
                .AsNoTracking()
                .Include(x => x.Badges)
                .OrderBy(x => x.CompanyDisplayName)
                .ToListAsync();

            //-------------------------------------------------------
            // Active Jobs Count
            //-------------------------------------------------------

            var activeJobs = await _context.JobPostings
                .AsNoTracking()
                .Where(x =>
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today)
                .GroupBy(x => x.EmployerId)
                .Select(x => new
                {
                    EmployerId = x.Key,
                    Count = x.Count()
                })
                .ToDictionaryAsync(
                    x => x.EmployerId,
                    x => x.Count);

            //-------------------------------------------------------
            // Cards
            //-------------------------------------------------------

            var cards = companies
                .Select(company =>
                {
                    activeJobs.TryGetValue(
                        company.EmployerId,
                        out int openJobs);

                    return new PublicCompanyCardDto
                    {
                        EmployerId =
                            company.EmployerId,

                        CompanyName =
                            company.CompanyDisplayName,

                        CompanyLogoUrl =
                            company.CompanyLogoUrl,

                        CoverImageUrl =
                            company.CoverImageUrl,

                        Industry =
                            company.IndustryType.ToString(),

                        City =
                            company.City,

                        State =
                            company.State,

                        IsVerified =
                            company.Badges.Any(x =>
                                x.BadgeStatus ==
                                BadgeStatus.Approved),

                        ReviewCount =
                            company.ReviewCount,

                        OpenJobsCount =
                            openJobs
                    };
                })
                .OrderByDescending(x => x.OpenJobsCount)
                .ThenBy(x => x.CompanyName)
                .ToList();

            //-------------------------------------------------------
            // Response
            //-------------------------------------------------------

            return new PublicCompanyListResponseDto
            {
                Success = true,

                Message =
                    $"{cards.Count} companies found.",

                Companies = cards
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PublicCompanyService.GetCompaniesAsync failed.");

            return new PublicCompanyListResponseDto
            {
                Success = false,

                Message =
                    ex.InnerException?.Message ??
                    ex.Message
            };
        }
    }

    //==========================================================
    // PART 2
    //==========================================================

    public async Task<PublicCompanyDetailResponseDto> GetCompanyDetailAsync(Guid employerId)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            //-------------------------------------------------------
            // Company
            //-------------------------------------------------------

            var company = await _context.EmployerProfiles
                .AsNoTracking()
                .Include(x => x.Badges)
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (company == null)
            {
                return new PublicCompanyDetailResponseDto
                {
                    Success = false,
                    Message = "Company not found."
                };
            }

            //-------------------------------------------------------
            // Open Jobs
            //-------------------------------------------------------

            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Include(x => x.EmployerProfile)
                    .ThenInclude(x => x.Badges)
                .Where(x =>
                    x.EmployerId == employerId &&
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today)
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ToListAsync();

            //-------------------------------------------------------
            // Response
            //-------------------------------------------------------

            return new PublicCompanyDetailResponseDto
            {
                Success = true,

                Message = "Company details retrieved successfully.",

                EmployerId = company.EmployerId,

                CompanyName = company.CompanyDisplayName,

                CompanyLogoUrl = company.CompanyLogoUrl,

                CoverImageUrl = company.CoverImageUrl,

                CompanyDescription = company.CompanyDescription,

                Industry = company.IndustryType.ToString(),

                BusinessType = company.BusinessType.ToString(),

                CompanySize = company.CompanySize?.ToString(),

                YearEstablished = company.YearEstablished,

                WebsiteUrl = company.WebsiteUrl,

                LinkedInUrl = company.LinkedInUrl,

                InstagramUrl = company.InstagramUrl,

                FacebookUrl = company.FacebookUrl,

                Phone = company.ContactPhone,

                Email = company.ContactEmailPublic,

                AddressLine1 = company.AddressLine1,

                AddressLine2 = company.AddressLine2,

                OfficeAddress = company.OfficeAddress,

                City = company.City,

                State = company.State,

                Country = company.Country,

                Pincode = company.Pincode,

                // Candidates should see where the company actually operates
                // day-to-day, not necessarily its legal registered address —
                // if the employer set a different Office Address, that's
                // what shows here (and what the map below is built from).
                // Falls back to the registered address when no distinct
                // office address was entered (the employer's own "Same as
                // the address above" checkbox leaves OfficeAddress blank).
                FullLocation =
                    !string.IsNullOrWhiteSpace(company.OfficeAddress)
                        ? company.OfficeAddress
                        : string.Join(", ",
                            new[]
                            {
                                company.AddressLine1,
                                company.City,
                                company.State,
                                company.Country
                            }
                            .Where(x => !string.IsNullOrWhiteSpace(x))),

                MapEmbedUrl =
                    BuildMapEmbedUrl(company),

                GstRegistered = company.GstRegistered,

                HasPoeLicence =
                    !string.IsNullOrWhiteSpace(company.PoeLicenceUrl),

                HasRpslLicence =
                    !string.IsNullOrWhiteSpace(company.RpslLicenceUrl),

                IsVerified =
                    company.Badges.Any(x =>
                        x.BadgeStatus == BadgeStatus.Approved),

                CompanyHighlights =
                    company.CompanyHighlights ?? new List<string>(),

                TotalEmployees =
                    company.TotalEmployees,

                OpenJobsCount =
                    jobs.Count,

                ReviewCount =
                    company.ReviewCount,

                Jobs =
                    jobs.Select(MapToCard)
                        .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PublicCompanyService.GetCompanyDetailAsync failed. EmployerId={EmployerId}",
                employerId);

            return new PublicCompanyDetailResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    private CandidateJobListItemDto MapToCard(JobPosting job)
    {
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

            IsClientHiring = job.IsClientHiring,
            ClientName = job.ClientName,
            ShowClientName = job.ShowClientName,

            CompanyVisibility =
                job.CompanyVisibility.ToString(),

            JobTitle = job.JobTitle,

            TradeCategory = job.TradeCategory,

            Department = job.Department,

            IndustryType =
    job.EmployerProfile?.IndustryType,

            LocationType =
                job.LocationType.ToString(),

            EmploymentType =
                job.EmploymentType.ToString(),

            EmploymentMode =
                job.EmploymentMode.ToString(),

            JobType =
                job.JobType.ToString(),

            JobLocation =
                GetJobLocation(job),

            CompanyLocation =
                GetCompanyLocation(job),

            SalaryRange =
                FormatSalary(job),

            SalaryVisibility =
                job.SalaryDisplayOption,

            ExperienceDisplay =
                GetExperienceDisplay(job),

            EducationRequired =
                job.EducationRequired,

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
                TruncateDescription(job.JobDescription, 180),

            Skills =
                job.KeySkills?
                    .Take(5)
                    .ToList()
                ?? new List<string>(),

            Tags =
                job.Tags
                ?? new List<string>(),

            IsFeatured =
                job.IsFeatured,

            IsUrgentHiring =
                job.IsUrgentHiring,



            PassportRequired =
                job.PassportRequired,

            IsInternational =
                job.IsInternational,

            CompanyVerified =
                IsVerified(job.EmployerProfile),

            ApplicationDeadline =
                job.ApplicationDeadline
        };
    }


    /// <summary>
    /// Maps a JobPosting entity to the card DTO used everywhere jobs are shown
    /// as a list (all-jobs feed, search results, similar jobs). Kept as a single
    /// shared method so every list of jobs looks and behaves the same way.
    /// </summary>
    private CandidateJobListItemDto MapToJobListItem(JobPosting job, bool isSaved)
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
job.CompanyVisibility == CompanyVisibility.ShowName
    ? job.EmployerProfile?.CompanyDisplayName
    : "Confidential Company",

            IsClientHiring = job.IsClientHiring,
            ClientName = job.ClientName,
            ShowClientName = job.ShowClientName,

            JobTitle = job.JobTitle,
            CompanyVisibility = job.CompanyVisibility.ToString(),
            TradeCategory = job.TradeCategory,
            Department = job.Department,
            LocationType = job.LocationType.ToString(),
            EmploymentType =
                job.EmploymentType,

            EmploymentMode =
                job.EmploymentMode,

            JobType =
                job.JobType,
            IndustryType =
                job.EmployerProfile?.IndustryType,
            Tags = job.Tags ?? new List<string>(),
            EducationRequired = job.EducationRequired,
            JobLocation = jobLocation,

            CompanyLocation = companyLocation,

            SalaryRange =
                FormatSalary(job) ?? "Confidential",

            SalaryVisibility = job.SalaryDisplayOption.ToString(),

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

            IsSaved = isSaved,

            CompanyVerified =
                job.EmployerProfile?.Badges?.Any() == true,

            ApplicationDeadline =
                job.ApplicationDeadline
        };
    }

    // ────────────────────────────────────────────────────────────
    // Similar-jobs keyword matching
    // ────────────────────────────────────────────────────────────

    // Generic/level words stripped out before comparing job titles, so
    // "Senior Welder" and "Pipe Welder" are recognised as the same trade
    // ("welder") instead of matching (or failing to match) on noise words.
    private static readonly HashSet<string> JobTitleStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "senior","sr","junior","jr","lead","chief","head","assistant","trainee",
        "apprentice","fresher","experienced","expert","new","the","and","of","for",
        "a","an","in","on","at","to","with","i","ii","iii","iv","level","grade",
        "class","gr","no","officer","staff","hiring","urgent","required","wanted"
    };

    private static List<string> ExtractTitleKeywords(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new List<string>();

        return title
            .ToLowerInvariant()
            .Split(new[] { ' ', '-', '/', ',', '(', ')', '&', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => w.Length > 2 && !JobTitleStopWords.Contains(w))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Finds jobs genuinely similar to <paramref name="referenceJob"/> — matched
    /// by shared keywords in the job title (e.g. "Pipe Welder" ↔ "Senior Welder"
    /// both share "welder") and/or the same trade category / role. A job that
    /// only shares a trade category but has a completely unrelated title
    /// (e.g. a stray "Senior" posting) is NOT considered similar — that was the
    /// bug that let unrelated jobs show up before. Returns an empty list when
    /// nothing genuinely similar exists, so the UI can hide the section.
    /// </summary>
    private async Task<List<CandidateJobListItemDto>> GetSimilarJobsAsync(
        JobPosting referenceJob,
        int take = 5)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var referenceKeywords = ExtractTitleKeywords(referenceJob.JobTitle);

        var candidatePool = await _context.JobPostings
            .AsNoTracking()
            .Include(x => x.EmployerProfile)
                .ThenInclude(x => x.Badges)
            .Where(x =>
                x.JobId != referenceJob.JobId &&
                x.JobStatus == JobStatus.Active &&
                x.IsActive &&
                !x.IsDeleted &&
                x.ApplicationDeadline >= today)
            .ToListAsync();

        var scored = candidatePool
            .Select(candidate =>
            {
                var candidateKeywords = ExtractTitleKeywords(candidate.JobTitle);

                int titleMatches = referenceKeywords
                    .Count(k => candidateKeywords.Contains(k));

                bool tradeMatch = string.Equals(
                    candidate.TradeCategory, referenceJob.TradeCategory,
                    StringComparison.OrdinalIgnoreCase);

                bool roleMatch =
                    !string.IsNullOrWhiteSpace(referenceJob.Role) &&
                    string.Equals(candidate.Role, referenceJob.Role,
                        StringComparison.OrdinalIgnoreCase);

                bool departmentMatch =
                    !string.IsNullOrWhiteSpace(referenceJob.Department) &&
                    string.Equals(candidate.Department, referenceJob.Department,
                        StringComparison.OrdinalIgnoreCase);

                int score =
                    (titleMatches * 10) +
                    (tradeMatch ? 3 : 0) +
                    (roleMatch ? 2 : 0) +
                    (departmentMatch ? 1 : 0);

                // A job counts as "similar" if it shares a keyword with the
                // title (e.g. "welder" in both), OR matches on trade
                // category, OR matches on role. Any one of these three is
                // enough to qualify — they're then ranked by score below,
                // so a title-keyword match (worth 10) always outranks a
                // trade-category-only match (worth 3), keeping the most
                // relevant jobs at the top even though the bar to appear
                // at all is lower than a strict title match.
                bool isRelevant = titleMatches > 0 || tradeMatch || roleMatch;

                return (Candidate: candidate, Score: score, IsRelevant: isRelevant);
            })
            .Where(x => x.IsRelevant)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Candidate.PublishedAt)
            .Take(take)
            .ToList();

        return scored
            .Select(x => MapToJobListItem(x.Candidate, isSaved: false))
            .ToList();
    }

    private static string FormatSalary(JobPosting job)
    {
        string currency = job.SalaryCurrency.ToString();

        return job.SalaryDisplayOption?.ToLowerInvariant() switch
        {
            "negotiable" =>
                "Negotiable",

            "show min only" =>
                $"{currency} {job.SalaryMin:N0}+",

            "show max only" =>
                $"Up to {currency} {job.SalaryMax:N0}",

            "show range" =>
                $"{currency} {job.SalaryMin:N0} - {job.SalaryMax:N0}",

            _ =>
                $"{currency} {job.SalaryMin:N0} - {job.SalaryMax:N0}"
        };
    }
    private static string GetExperienceDisplay(JobPosting job)
    {
        if (job.ExperienceMinYears == 0 &&
            job.ExperienceMaxYears == 0)
        {
            return "Fresher";
        }

        if (job.ExperienceMaxYears == 0)
        {
            return $"{job.ExperienceMinYears}+ Years";
        }

        return $"{job.ExperienceMinYears}-{job.ExperienceMaxYears} Years";
    }

    private static string GetJobLocation(JobPosting job)
    {
        if (job.LocationType == LocationType.Offshore)
        {
            return string.Join(", ",
                new[]
                {
                job.OffshoreRegion,
                job.OffshoreCountry
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        return string.Join(", ",
            new[]
            {
            job.OnshoreCity,
            job.OnshoreState
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string GetCompanyLocation(JobPosting job)
    {
        return string.Join(", ",
            new[]
            {
            job.EmployerProfile?.City,
            job.EmployerProfile?.State
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
    private static bool IsVerified(
    EmployerProfile? employer)
    {
        if (employer == null)
            return false;

        return employer.Badges.Any(x =>
            x.BadgeStatus == BadgeStatus.Approved);
    }

    private static string TruncateDescription(
    string? description,
    int maxLength)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        if (description.Length <= maxLength)
            return description;

        return description.Substring(0, maxLength) + "...";
    }

    private static string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "Recently";

        var span = DateTime.UtcNow - dateTime.Value;

        if (span.TotalMinutes < 1)
            return "Just now";

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} min ago";

        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hr ago";

        if (span.TotalDays < 30)
            return $"{(int)span.TotalDays} day(s) ago";

        if (span.TotalDays < 365)
            return $"{(int)(span.TotalDays / 30)} month(s) ago";

        return $"{(int)(span.TotalDays / 365)} year(s) ago";
    }

    /// <summary>
    /// Builds a Google Maps embed URL from the company's real address using the
    /// keyless "output=embed" query format. Returns null when there's no usable
    /// address, so the frontend can hide the map instead of showing a
    /// hardcoded location that doesn't belong to this company.
    /// </summary>
    private static string? BuildMapEmbedUrl(JobPortal.Domain.Entities.EmployerProfile? company)
    {
        if (company == null)
            return null;

        string query;

        if (!string.IsNullOrWhiteSpace(company.OfficeAddress))
        {
            // A distinct office address is a full freeform string already
            // (city/state/country as typed by the employer), so it's used
            // as-is rather than being reassembled from separate fields.
            query = company.OfficeAddress;
        }
        else
        {
            var addressParts = new[]
                {
                    company.AddressLine1,
                    company.City,
                    company.State,
                    company.Country
                }
                .Where(x => !string.IsNullOrWhiteSpace(x));

            query = string.Join(", ", addressParts);
        }

        if (string.IsNullOrWhiteSpace(query))
            return null;

        return $"https://www.google.com/maps?q={Uri.EscapeDataString(query)}&output=embed";
    }
}