    using global::JobPortal.Application.DTOs.Recruiter;
    using global::JobPortal.Domain.Entities;
    using global::JobPortal.Infrastructure.Persistence;
    using global::JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Application.DTOs.JobPosting;
using JobPortal.Domain.Enums.common;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    namespace JobPortal.Services.Implement.Recruiter { 

    public class JobPostingService : IJobPostingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobPostingService> _logger;

        private static readonly Dictionary<int, string> StepNames = new()
    {
        { 1, "Job Details" },
        { 2, "Compensation" },
        { 3, "Skills & JD" },
        { 4, "Eligibility" },
        { 5, "Location" },
        { 6, "Questions" },
        { 7, "Publishing" }
    };

        public JobPostingService(
            AppDbContext context,
            ILogger<JobPostingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════
        // STEP 1 — Job Details → creates Draft job in DB
        // ════════════════════════════════════════════════
        public async Task<JobDetailsResponseDto> SaveJobDetailsAsync(
            JobDetailsRequestDto request, Guid employerId)
        {
            try
            {
                // ── Validate employer exists and is active ─────
                var employer = await _context.EmployerProfiles
    .FirstOrDefaultAsync(e =>
        e.EmployerId == employerId);

                if (employer == null)
                    return new JobDetailsResponseDto
                    {
                        Success = false,
                        Message = "Employer account not found or not active."
                    };

                // ── Create Draft job immediately ───────────────
                var job = new JobPosting
                {
                    JobId = Guid.NewGuid(),
                    EmployerId = employerId,
                    JobTitle = request.JobTitle,
                    TradeCategory = request.TradeCategory,
                    Role = request.Role,
                    ExperienceRequiredYears = (byte)request.ExperienceRequiredYears,
                    JobDescription = request.JobDescription,
                    JobStatus = "Draft",
                    CurrentStep = 1,
                    LastCompletedStep = 1,
                    ApplicationDeadline = DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(30)),  // default 30 days
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.JobPostings.Add(job);
                await _context.SaveChangesAsync();      // ✅ saved immediately

                _logger.LogInformation(
                    "Step1 saved — JobId:{JobId} Employer:{EId}",
                    job.JobId, employerId);

                return new JobDetailsResponseDto
                {
                    Success = true,
                    Message = "Job details saved as draft.",
                    JobId = job.JobId,
                    JobStatus = "Draft",
                    StepStatus = BuildStepStatus(job)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save job details error.");

                return new JobDetailsResponseDto
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        // ════════════════════════════════════════════════
        // STEP 2 — Compensation
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveCompensationAsync(
            CompensationRequestDto request, Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null)
                    return Fail("Job not found.");

                if (request.SalaryMin > request.SalaryMax)
                    return Fail("Min salary cannot be greater than max salary.");

                job.SalaryMin = request.SalaryMin;
                job.SalaryMax = request.SalaryMax;
                job.SalaryCurrency = request.SalaryCurrency.ToString();        // ✅
                job.SalaryDisplayOption = request.SalaryDisplayOption.ToString(); // ✅
                job.CurrentStep = 2;
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 2);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();      

                return Ok(job, "Compensation saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save compensation error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 3 — Skills & JD
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveSkillsAsync(
            SkillsRequestDto request, Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null) return Fail("Job not found.");

                job.KeySkills = JsonSerializer.Serialize(request.KeySkills);
                job.LicenceDocsRequired = request.LicenceDocsRequired;
                job.LanguageRequired = request.LanguageRequired;

                // Append additional description to main description
                if (!string.IsNullOrWhiteSpace(request.AdditionalJobDescription))
                    job.JobDescription += $"\n\n{request.AdditionalJobDescription}";

                job.CurrentStep = 3;
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 3);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();      // ✅ saved immediately

                return Ok(job, "Skills saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save skills error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 4 — Eligibility
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveEligibilityAsync(
            EligibilityRequestDto request, Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null) return Fail("Job not found.");

                // ── Age validation ─────────────────────────────
                if (request.AgeMin.HasValue && request.AgeMax.HasValue
                    && request.AgeMin > request.AgeMax)
                    return Fail("Min age cannot be greater than max age.");

                // ── Passport months needed if passport required ─
                if (request.PassportRequired && !request.PassportValidityMonths.HasValue)
                    return Fail("Passport validity months required when passport is required.");

                job.Vacancies = (short)request.Vacancies;
                job.EducationRequired = request.EducationRequired.ToString();  // ✅
                job.AgeMin = request.AgeMin.HasValue ? (byte)request.AgeMin.Value : null;
                job.AgeMax = request.AgeMax.HasValue ? (byte)request.AgeMax.Value : null;
                job.GenderPreferred = request.GenderPreferred.ToString();      // ✅
                job.DisabilityEligible = request.DisabilityEligible;
                job.PassportRequired = request.PassportRequired;
                job.PassportValidityMonths = request.PassportValidityMonths.HasValue
                    ? (byte)request.PassportValidityMonths.Value : null;
                job.CurrentStep = 4;
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 4);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();      // ✅ saved immediately

                return Ok(job, "Eligibility saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save eligibility error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 5 — Location
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveLocationAsync(
            LocationRequestDto request, Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null) return Fail("Job not found.");

                // ── Location type validation ───────────────────
                if (request.LocationType == LocationType.Onshore)  // ✅ enum comparison in DTO is fine
                {
                    if (string.IsNullOrWhiteSpace(request.OnshoreCity))
                        return Fail("City is required for onshore jobs.");
                    if (string.IsNullOrWhiteSpace(request.OnshoreState))
                        return Fail("State is required for onshore jobs.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.OffshoreRegion))
                        return Fail("Offshore region is required for offshore jobs.");
                }

                job.LocationType = request.LocationType.ToString();
                job.OnshoreCity = request.OnshoreCity;
                job.OnshoreState = request.OnshoreState;
                job.OffshoreVesselName = request.OffshoreVesselName;
                job.OffshoreRegion = request.OffshoreRegion;
                job.IsInternational = request.LocationType == LocationType.Offshore
                    || job.PassportRequired;
                job.CurrentStep = 5;
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 5);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();      // ✅ saved immediately

                return Ok(job, "Location saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save location error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 6 — Screening Questions
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveQuestionsAsync(
            QuestionsRequestDto request, Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null) return Fail("Job not found.");

                if (request.Questions.Count > 5)
                    return Fail("Maximum 5 screening questions allowed.");

                // Store questions as JSON in a new column
                job.ScreeningQuestions = JsonSerializer.Serialize(request.Questions);
                job.CurrentStep = 6;
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 6);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();      // ✅ saved immediately

                return Ok(job, "Questions saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save questions error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 7 — Publish or Save Draft
        // ════════════════════════════════════════════════
        public async Task<PublishingResponseDto> PublishJobAsync(
            PublishingRequestDto request, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(request.JobId, employerId);
                if (job == null)
                    return new PublishingResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };

                // ── Validate minimum required steps ───────────
                if (job.LastCompletedStep < 5)
                    return new PublishingResponseDto
                    {
                        Success = false,
                        Message = $"Please complete all required steps. Last completed: Step {job.LastCompletedStep} ({StepNames[job.LastCompletedStep]})."
                    };

                job.ApplicationDeadline = request.ApplicationDeadline;
                job.CompanyVisibility = request.CompanyVisibility.ToString();
                job.PublishingTags = JsonSerializer.Serialize(request.PublishingTags);
                job.LastCompletedStep = 7;
                job.UpdatedAt = DateTime.UtcNow;

                if (request.PublishNow)
                {
                    job.JobStatus = "Active";
                    job.PublishedAt = DateTime.UtcNow;
                }
                else
                {
                    job.JobStatus = "Draft";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job {Status} — JobId:{JobId} Employer:{EId}",
                    job.JobStatus, job.JobId, employerId);

                return new PublishingResponseDto
                {
                    Success = true,
                    Message = request.PublishNow
                        ? "Job published successfully!"
                        : "Job saved as draft.",
                    JobId = job.JobId,
                    JobStatus = job.JobStatus,
                    PublishedAt = job.PublishedAt,
                    JobUrl = request.PublishNow
                        ? $"/jobs/{job.JobId}"
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Publish job error.");
                return new PublishingResponseDto
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                };
            }
        }

        // ════════════════════════════════════════════════
        // SAVE DRAFT — callable at any step
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveDraftAsync(
            Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null) return Fail("Job not found.");

                job.JobStatus = "Draft";
                job.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(job, "Draft saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save draft error.");
                return Fail("An error occurred.");
            }
        }

        // ════════════════════════════════════════════════
        // ROLE SEARCH — returns suggestions from existing jobs
        // ════════════════════════════════════════════════
        public async Task<RoleSearchResponseDto> SearchRolesAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return new RoleSearchResponseDto
                    {
                        Suggestions = new List<string>(),
                        AllowCustom = true,
                        Message = "Type at least 2 characters."
                    };

                // Search existing trade categories and roles
                var fromTrade = await _context.JobPostings
                    .Where(j => j.TradeCategory
                        .ToLower().Contains(query.ToLower()))
                    .Select(j => j.TradeCategory)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                var fromRole = await _context.JobPostings
                    .Where(j => j.Role != null &&
                        j.Role.ToLower().Contains(query.ToLower()))
                    .Select(j => j.Role!)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                var suggestions = fromTrade
                    .Union(fromRole)
                    .Distinct()
                    .Take(8)
                    .ToList();

                return new RoleSearchResponseDto
                {
                    Suggestions = suggestions,
                    AllowCustom = true,   // always allow custom entry
                    Message = suggestions.Count == 0
                        ? "No matches found. You can type your own."
                        : $"{suggestions.Count} suggestion(s) found."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Role search error.");
                return new RoleSearchResponseDto
                {
                    Suggestions = new List<string>(),
                    AllowCustom = true
                };
            }
        }

        // ════════════════════════════════════════════════
        // RESUME — get existing draft job progress
        // ════════════════════════════════════════════════
        public async Task<ResumeJobResponseDto> ResumeJobAsync(
            Guid jobId, Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);
                if (job == null)
                    return new ResumeJobResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };

                return new ResumeJobResponseDto
                {
                    Success = true,
                    Message = $"Resume from Step {job.LastCompletedStep + 1} ({StepNames.GetValueOrDefault(job.LastCompletedStep + 1, "Publishing")}).",
                    JobId = job.JobId,
                    StepStatus = BuildStepStatus(job),
                    Step1Data = new JobDetailsRequestDto
                    {
                        JobTitle = job.JobTitle,
                        TradeCategory = job.TradeCategory,
                        Role = job.Role,
                        ExperienceRequiredYears = job.ExperienceRequiredYears,
                        JobDescription = job.JobDescription
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resume job error.");
                return new ResumeJobResponseDto
                {
                    Success = false,
                    Message = "An error occurred."
                };
            }
        }

        // ── Private Helpers ───────────────────────────────────
        private async Task<JobPosting?> GetJobAsync(Guid jobId, Guid employerId) =>
      await _context.JobPostings
          .FirstOrDefaultAsync(j =>
              j.JobId == jobId &&
              j.EmployerId == employerId &&
              j.JobStatus != "Archived");

        private static JobStepStatusDto BuildStepStatus(JobPosting job)
        {
            var completed = Enumerable.Range(1, job.LastCompletedStep)
                .Select(i => StepNames[i])
                .ToList();

            var next = job.LastCompletedStep + 1;

            return new JobStepStatusDto
            {
                JobId = job.JobId,
                CurrentStep = job.CurrentStep,
                LastCompletedStep = job.LastCompletedStep,
                TotalSteps = 7,
                JobStatus = job.JobStatus,
                CompletedSteps = completed,
                NextStep = next <= 7 ? StepNames[next] : "Done"
            };
        }

        private static BaseJobResponseDto Ok(JobPosting job, string message) =>
            new()
            {
                Success = true,
                Message = message,
                JobId = job.JobId,
                StepStatus = BuildStepStatus(job)
            };

        private static BaseJobResponseDto Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
