// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateJobService.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Domain.Entities;
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
            // ── Clamp pagination ──────────────────────────────
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            // ── Base query: only Published/Active jobs ────────
            var query = _context.JobPostings
                .Include(j => j.EmployerProfile)
                .Where(j => j.JobStatus == "Active")
                .AsQueryable();

            // ── Keyword search ────────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim().ToLower();
                query = query.Where(j =>
                    j.JobTitle.ToLower().Contains(kw) ||
                    j.TradeCategory.ToLower().Contains(kw) ||
                    j.JobDescription.ToLower().Contains(kw) ||
                    (j.Role != null && j.Role.ToLower().Contains(kw)) ||
                    (j.KeySkills != null && j.KeySkills.ToLower().Contains(kw)) ||
                    j.EmployerProfile.CompanyDisplayName.ToLower().Contains(kw));
            }

            // ── Location filters ──────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                var loc = request.Location.Trim().ToLower();
                query = query.Where(j =>
                    (j.OnshoreCity != null && j.OnshoreCity.ToLower().Contains(loc)) ||
                    (j.OnshoreState != null && j.OnshoreState.ToLower().Contains(loc)) ||
                    (j.OffshoreRegion != null && j.OffshoreRegion.ToLower().Contains(loc)));
            }

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                var st = request.State.Trim().ToLower();
                query = query.Where(j =>
                    j.OnshoreState != null && j.OnshoreState.ToLower().Contains(st));
            }

            if (!string.IsNullOrWhiteSpace(request.LocationType))
                query = query.Where(j => j.LocationType == request.LocationType);

            // ── Trade / role filters ──────────────────────────
            if (!string.IsNullOrWhiteSpace(request.TradeCategory))
            {
                var tc = request.TradeCategory.Trim().ToLower();
                query = query.Where(j => j.TradeCategory.ToLower().Contains(tc));
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var rl = request.Role.Trim().ToLower();
                query = query.Where(j => j.Role != null && j.Role.ToLower().Contains(rl));
            }

            // ── Employment / job type ─────────────────────────
            // JobType is stored in PublishingTags JSON; we handle via Tags check below
            if (!string.IsNullOrWhiteSpace(request.EmploymentType))
                query = query.Where(j =>
                    j.PublishingTags != null &&
                    j.PublishingTags.Contains(request.EmploymentType));

            // ── Experience ────────────────────────────────────
            if (request.ExperienceYearsMin.HasValue)
                query = query.Where(j => j.ExperienceRequiredYears >= request.ExperienceYearsMin.Value);

            if (request.ExperienceYearsMax.HasValue)
                query = query.Where(j => j.ExperienceRequiredYears <= request.ExperienceYearsMax.Value);

            // ── Salary ────────────────────────────────────────
            if (request.SalaryMin.HasValue)
                query = query.Where(j =>
                    j.SalaryDisplayOption != "Confidential" &&
                    j.SalaryMax >= request.SalaryMin.Value);

            if (request.SalaryMax.HasValue)
                query = query.Where(j =>
                    j.SalaryDisplayOption != "Confidential" &&
                    j.SalaryMin <= request.SalaryMax.Value);

            if (!string.IsNullOrWhiteSpace(request.SalaryCurrency))
                query = query.Where(j => j.SalaryCurrency == request.SalaryCurrency);

            // ── Eligibility ───────────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.Gender) && request.Gender != "Any")
                query = query.Where(j => j.GenderPreferred == request.Gender || j.GenderPreferred == "Any");

            if (!string.IsNullOrWhiteSpace(request.EducationLevel))
                query = query.Where(j => j.EducationRequired == request.EducationLevel);

            if (request.DisabilityEligible.HasValue)
                query = query.Where(j => j.DisabilityEligible == request.DisabilityEligible.Value);

            if (request.PassportRequired.HasValue)
                query = query.Where(j => j.PassportRequired == request.PassportRequired.Value);

            // ── Freshness (posted within N days) ──────────────
            if (request.PostedWithinDays.HasValue)
            {
                var cutoff = DateTime.UtcNow.AddDays(-request.PostedWithinDays.Value);
                query = query.Where(j => j.PublishedAt != null && j.PublishedAt >= cutoff);
            }

            // ── Deadline not passed ───────────────────────────
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(j => j.ApplicationDeadline >= today);

            // ── Sort ──────────────────────────────────────────
            query = request.Sort switch
            {
                "oldest" => query.OrderBy(j => j.PublishedAt),
                "salary_high" => query.OrderByDescending(j => j.SalaryMax),
                "salary_low" => query.OrderBy(j => j.SalaryMin),
                _ => query.OrderByDescending(j => j.PublishedAt)  // "newest" default
            };

            // ── Total count (before paging) ───────────────────
            var totalCount = await query.CountAsync();

            // ── Paginate ──────────────────────────────────────
            var jobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new CandidateJobListResponseDto
            {
                Success = true,
                Message = $"{totalCount} job(s) found.",
                Jobs = jobs.Select(j => MapToCard(j)).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                AppliedFilters = request
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CandidateJobService.GetJobsAsync error.");
            return new CandidateJobListResponseDto
            {
                Success = false,
                Message = "An error occurred while fetching jobs."
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
                    j.JobStatus == "Active" &&
                    j.ApplicationDeadline >= DateOnly.FromDateTime(DateTime.UtcNow));

            if (job == null)
                return new CandidateJobDetailResponseDto
                {
                    Success = false,
                    Message = "Job not found or no longer active."
                };

            var employer = job.EmployerProfile;
            var isConfidential = job.CompanyVisibility == "Confidential_Client";

            // ── Parse stored JSON fields ───────────────────────
            var skills = ParseJsonList(job.KeySkills);
            var screeningQuestions = ParseScreeningQuestions(job.ScreeningQuestions);
            var publishingTags = ParseJsonList(job.PublishingTags);

            // ── Similar jobs (same trade, different job) ──────
            var similarJobs = await _context.JobPostings
                .Include(j => j.EmployerProfile)
                .Where(j =>
                    j.JobId != jobId &&
                    j.JobStatus == "Active" &&
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

                // ── Company ──────────────────────────────────
                CompanyName = isConfidential ? null : employer.CompanyDisplayName,
                CompanyLogoUrl = isConfidential ? null : employer.CompanyLogoUrl,
                IsConfidentialCompany = isConfidential,
                CompanyWebsite = isConfidential ? null : employer.WebsiteUrl,
                CompanyDescription = isConfidential ? null : employer.CompanyDescription,
                CompanyCity = isConfidential ? null : employer.City,
                CompanyState = isConfidential ? null : employer.State,
                CompanyAddress = isConfidential ? null : employer.AddressLine1,
                CompanyPhone = isConfidential ? null : employer.ContactPhone,
                CompanyEmail = isConfidential ? null : employer.ContactEmailPublic,
                CompanyIndustry = employer.IndustryType.ToString(),
                CompanySize = employer.CompanySize?.ToString(),
                HasPoeLicence = !string.IsNullOrWhiteSpace(employer.PoeLicenceS3Url),
                HasRpslLicence = !string.IsNullOrWhiteSpace(employer.RpslLicenceS3Url),

                // ── Job basics ────────────────────────────────
                JobTitle = job.JobTitle,
                TradeCategory = job.TradeCategory,
                Role = job.Role,
                JobType = GetJobTypeFromTags(publishingTags),
                EmploymentType = GetEmploymentTypeFromTags(publishingTags),

                // ── Description ───────────────────────────────
                JobDescription = job.JobDescription,

                // ── Location ──────────────────────────────────
                LocationType = job.LocationType,
                City = job.OnshoreCity,
                State = job.OnshoreState,
                OffshoreVesselName = job.OffshoreVesselName,
                OffshoreRegion = job.OffshoreRegion,
                IsInternational = job.IsInternational,

                // ── Salary ────────────────────────────────────
                SalaryDisplay = FormatSalary(job),
                SalaryMin = job.SalaryDisplayOption == "Confidential" ? null : job.SalaryMin,
                SalaryMax = job.SalaryDisplayOption == "Confidential" ? null : job.SalaryMax,
                SalaryCurrency = job.SalaryCurrency,

                // ── Skills & experience ────────────────────────
                ExperienceRequiredYears = job.ExperienceRequiredYears,
                KeySkills = skills,
                LicenceDocsRequired = job.LicenceDocsRequired,
                LanguageRequired = job.LanguageRequired,

                // ── Eligibility ───────────────────────────────
                Vacancies = job.Vacancies,
                EducationRequired = job.EducationRequired,
                AgeMin = job.AgeMin,
                AgeMax = job.AgeMax,
                GenderPreferred = job.GenderPreferred,
                DisabilityEligible = job.DisabilityEligible,
                PassportRequired = job.PassportRequired,
                PassportValidityMonths = job.PassportValidityMonths,

                // ── Deadline & meta ───────────────────────────
                ApplicationDeadline = job.ApplicationDeadline,
                IsDeadlineSoon = (job.ApplicationDeadline.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays <= 7,
                PublishedAt = job.PublishedAt,
                TimeAgo = GetTimeAgo(job.PublishedAt),
                AppliedCount = job.AppliedCount,
                Tags = BuildTags(job, publishingTags),

                // ── Screening questions ────────────────────────
                ScreeningQuestions = screeningQuestions,

                // ── Similar jobs ──────────────────────────────
                SimilarJobs = similarJobs.Select(j => MapToCard(j)).ToList()
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
                .AnyAsync(j => j.JobId == jobId && j.JobStatus == "Active");

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
                .Where(j => j.JobStatus == "Active" && j.ApplicationDeadline >= today)
                .ToListAsync();

            return new JobFilterOptionsResponseDto
            {
                Success = true,

                TradeCategories = activeJobs
                    .Select(j => j.TradeCategory)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                Roles = activeJobs
                    .Where(j => j.Role != null)
                    .Select(j => j.Role!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                Cities = activeJobs
                    .Where(j => j.OnshoreCity != null)
                    .Select(j => j.OnshoreCity!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                States = activeJobs
                    .Where(j => j.OnshoreState != null)
                    .Select(j => j.OnshoreState!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),

                LocationTypes = activeJobs
                    .Select(j => j.LocationType)
                    .Distinct()
                    .ToList(),

                EmploymentTypes = new List<string>
                {
                    "Permanent", "Contract", "Temporary", "Internship"
                },

                EducationLevels = activeJobs
                    .Where(j => j.EducationRequired != null)
                    .Select(j => j.EducationRequired!)
                    .Distinct()
                    .ToList(),

                Currencies = activeJobs
                    .Select(j => j.SalaryCurrency)
                    .Distinct()
                    .ToList(),

                GenderOptions = new List<string> { "Male", "Female", "Any" },

                MaxSalary = activeJobs
                    .Where(j => j.SalaryDisplayOption != "Confidential")
                    .Select(j => j.SalaryMax)
                    .DefaultIfEmpty(0)
                    .Max(),

                MaxExperienceYears = activeJobs
                    .Select(j => (int)j.ExperienceRequiredYears)
                    .DefaultIfEmpty(0)
                    .Max(),

                TotalActiveJobs = activeJobs.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFilterOptionsAsync error.");
            return new JobFilterOptionsResponseDto { Success = false };
        }
    }

    // ════════════════════════════════════════════════════════
    // ── Private helpers ──────────────────────────────────────
    // ════════════════════════════════════════════════════════

    /// <summary>Map a <see cref="JobPosting"/> to a compact card DTO.</summary>
    private static CandidateJobCardDto MapToCard(JobPosting job)
    {
        var isConfidential = job.CompanyVisibility == "Confidential_Client";
        var publishingTags = ParseJsonList(job.PublishingTags);
        var skills = ParseJsonList(job.KeySkills);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new CandidateJobCardDto
        {
            JobId = job.JobId,

            // ── Company ──────────────────────────────────
            CompanyName = isConfidential ? null : job.EmployerProfile?.CompanyDisplayName,
            CompanyLogoUrl = isConfidential ? null : job.EmployerProfile?.CompanyLogoUrl,
            IsConfidentialCompany = isConfidential,

            // ── Job basics ────────────────────────────────
            JobTitle = job.JobTitle,
            TradeCategory = job.TradeCategory,
            Role = job.Role,
            JobType = GetJobTypeFromTags(publishingTags),
            EmploymentType = GetEmploymentTypeFromTags(publishingTags),

            // ── Location ──────────────────────────────────
            LocationType = job.LocationType,
            City = job.OnshoreCity,
            State = job.OnshoreState,
            OffshoreRegion = job.OffshoreRegion,
            IsInternational = job.IsInternational,

            // ── Salary ────────────────────────────────────
            SalaryDisplay = FormatSalary(job),
            SalaryMin = job.SalaryDisplayOption == "Confidential" ? null : job.SalaryMin,
            SalaryMax = job.SalaryDisplayOption == "Confidential" ? null : job.SalaryMax,
            SalaryCurrency = job.SalaryCurrency,

            // ── Experience & eligibility ──────────────────
            ExperienceRequiredYears = job.ExperienceRequiredYears,
            EducationRequired = job.EducationRequired,
            GenderPreferred = job.GenderPreferred,
            DisabilityEligible = job.DisabilityEligible,
            PassportRequired = job.PassportRequired,

            // ── Openings & deadline ───────────────────────
            Vacancies = job.Vacancies,
            ApplicationDeadline = job.ApplicationDeadline,
            IsDeadlineSoon =
                (job.ApplicationDeadline.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays <= 7,

            // ── Meta ──────────────────────────────────────
            Tags = BuildTags(job, publishingTags),
            KeySkills = skills.Take(5).ToList(),   // show up to 5 in card
            TimeAgo = GetTimeAgo(job.PublishedAt),
            PublishedAt = job.PublishedAt,
            AppliedCount = job.AppliedCount,

            // ── Short description snippet (first 160 chars) ─
            ShortDescription = TruncateDescription(job.JobDescription, 160)
        };
    }

    // ── Salary formatting ─────────────────────────────────
    private static string? FormatSalary(JobPosting job)
    {
        if (job.SalaryDisplayOption == "Confidential") return null;

        var symbol = job.SalaryCurrency switch
        {
            "USD" => "$",
            "AED" => "AED ",
            "SAR" => "SAR ",
            _ => "₹"
        };

        return job.SalaryDisplayOption == "Show_Min_Only"
            ? $"{symbol}{job.SalaryMin:N0}+"
            : $"{symbol}{job.SalaryMin:N0} – {symbol}{job.SalaryMax:N0} / month";
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
}