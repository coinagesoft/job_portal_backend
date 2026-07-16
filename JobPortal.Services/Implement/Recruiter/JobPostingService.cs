    using global::JobPortal.Application.DTOs.Recruiter;
    using global::JobPortal.Domain.Entities;
    using global::JobPortal.Infrastructure.Persistence;
    using global::JobPortal.Services.IImplement.IRecruiter;
    using JobPortal.Application.DTOs.JobPosting;
    using JobPortal.Domain.Enums.common;
    using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Services.IImplement.AI;
using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    namespace JobPortal.Services.Implement.Recruiter { 

    public class JobPostingService : IJobPostingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobPostingService> _logger;
        private readonly IEmbeddingStorageService _embeddingStorage;   


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
            ILogger<JobPostingService> logger,
            IEmbeddingStorageService embeddingStorage)                  // ✅ नवीन param

        {
            _context = context;
            _logger = logger;
            _embeddingStorage = embeddingStorage;
        }

        // ════════════════════════════════════════════════
        // STEP 1 — Job Details → creates Draft job in DB
        // ════════════════════════════════════════════════
        //    public async Task<JobDetailsResponseDto> SaveJobDetailsAsync(
        //        JobDetailsRequestDto request, Guid employerId)
        //    {
        //        try
        //        {
        //            // ── Validate employer exists and is active ─────
        //            var employer = await _context.EmployerProfiles
        //.FirstOrDefaultAsync(e =>
        //    e.EmployerId == employerId);

        //            if (employer == null)
        //                return new JobDetailsResponseDto
        //                {
        //                    Success = false,
        //                    Message = "Employer account not found or not active."
        //                };

        //            // ── Create Draft job immediately ───────────────
        //            var job = new JobPosting
        //            {
        //                JobId = Guid.NewGuid(),
        //                EmployerId = employerId,
        //                JobTitle = request.JobTitle,
        //                TradeCategory = request.TradeCategory,
        //                Role = request.Role,
        //                ExperienceRequiredYears = (byte)request.ExperienceRequiredYears,
        //                JobDescription = request.JobDescription,
        //                JobStatus = "Draft",
        //                CurrentStep = 1,
        //                LastCompletedStep = 1,
        //                ApplicationDeadline = DateOnly.FromDateTime(
        //                    DateTime.UtcNow.AddDays(30)),  // default 30 days
        //                CreatedAt = DateTime.UtcNow,
        //                UpdatedAt = DateTime.UtcNow
        //            };

        //            _context.JobPostings.Add(job);
        //            await _context.SaveChangesAsync();      // ✅ saved immediately

        //            _logger.LogInformation(
        //                "Step1 saved — JobId:{JobId} Employer:{EId}",
        //                job.JobId, employerId);

        //            return new JobDetailsResponseDto
        //            {
        //                Success = true,
        //                Message = "Job details saved as draft.",
        //                JobId = job.JobId,
        //                JobStatus = "Draft",
        //                StepStatus = BuildStepStatus(job)
        //            };
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Save job details error.");

        //            return new JobDetailsResponseDto
        //            {
        //                Success = false,
        //                Message = ex.InnerException?.Message ?? ex.Message
        //            };
        //        }
        //    }

        // ════════════════════════════════════════════════
        // STEP 2 — Compensation
        // ════════════════════════════════════════════════


        public async Task<JobDetailsResponseDto> SaveJobDetailsAsync(
           JobDetailsRequestDto request,
           Guid employerId)
        {
            try
            {
                var employer = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(e => e.EmployerId == employerId);

                if (employer == null)
                {
                    return new JobDetailsResponseDto
                    {
                        Success = false,
                        Message = $"Employer not found. EmployerId: {employerId}"
                    };
                }

                JobPosting job;

                // =====================================================
                // CREATE
                // =====================================================

                job = await _context.JobPostings
                     .FirstOrDefaultAsync(x =>
                      x.JobId == request.JobId &&
                     x.EmployerId == employerId);

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = Guid.NewGuid(),
                        EmployerId = employerId,

                        JobTitle = request.JobTitle ?? string.Empty,
                        TradeCategory = request.TradeCategory ?? string.Empty,
                        Role = request.Role,
                        JobDescription = request.JobDescription ?? string.Empty,
                        SalaryMin = 0,
                        SalaryMax = 0,
                        SalaryCurrency = "INR",
                        SalaryDisplayOption = "Show Range",
                        ExperienceMinYears = (byte)(request.ExperienceMinYears ?? 0),
                        ExperienceMaxYears = (byte)(request.ExperienceMaxYears ?? 0),

                        // Job
                        JobType = string.IsNullOrWhiteSpace(request.JobType)
                            ? "Normal Job"
                            : request.JobType.Trim(),

                        // Employment
                        EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType)
                            ? "Full Time"
                            : request.EmploymentType.Trim(),

                        EmploymentMode = string.IsNullOrWhiteSpace(request.EmploymentMode)
                            ? "Onsite"
                            : request.EmploymentMode.Trim(),

                        Department = request.Department,
                        IndustryType = request.IndustryType,
                        DutyHoursPerDay = request.DutyHoursPerDay.HasValue
                            ? (byte?)request.DutyHoursPerDay.Value
                            : null,

                        PaidOvertime = request.PaidOvertime ?? false,

                        KeyResponsibilities = request.KeyResponsibilities,

                        // Salary
                       

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 1,
                        LastCompletedStep = 1,

                        ApplicationDeadline = DateOnly.FromDateTime(
                            DateTime.UtcNow.AddDays(30)),

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,

                        IsActive = true,
                        IsDeleted = false,

                        ViewCount = 0,
                        AppliedCount = 0
                    };

                    _context.JobPostings.Add(job);
                }
                // =====================================================
                // UPDATE
                // =====================================================
                else
                {
                    job = await _context.JobPostings
                        .FirstOrDefaultAsync(x =>
                            x.JobId == request.JobId &&
                            x.EmployerId == employerId);

                    if (job == null)
                    {
                        return new JobDetailsResponseDto
                        {
                            Success = false,
                            Message = "Job not found."
                        };
                    }

                    job.JobTitle = request.JobTitle ?? job.JobTitle;
                    job.TradeCategory = request.TradeCategory ?? job.TradeCategory;
                    job.Role = request.Role;
                    job.JobDescription = request.JobDescription ?? job.JobDescription;

                  
                    job.ExperienceMinYears =
                        (byte)(request.ExperienceMinYears ?? job.ExperienceMinYears);

                    job.ExperienceMaxYears =
                        (byte)(request.ExperienceMaxYears ?? job.ExperienceMaxYears);

                    if (!string.IsNullOrWhiteSpace(request.JobType))
                    {
                        job.JobType = request.JobType.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.EmploymentType))
                    {
                        job.EmploymentType = request.EmploymentType.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.EmploymentMode))
                    {
                        job.EmploymentMode = request.EmploymentMode.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.Department))
                    {
                        job.Department = request.Department.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.IndustryType))
                    {
                        job.IndustryType = request.IndustryType.Trim();
                    }

                    if (request.DutyHoursPerDay.HasValue)
                    {
                        job.DutyHoursPerDay = (byte)request.DutyHoursPerDay.Value;
                    }

                    if (request.PaidOvertime.HasValue)
                    {
                        job.PaidOvertime = request.PaidOvertime.Value;
                    }

                    if (request.KeyResponsibilities != null)
                    {
                        job.KeyResponsibilities = request.KeyResponsibilities;
                    }

                   

                   

                    job.UpdatedAt = DateTime.UtcNow;

                    job.CurrentStep = Math.Max(job.CurrentStep, 1);
                    job.LastCompletedStep = Math.Max(job.LastCompletedStep, 1);
                }

                await _context.SaveChangesAsync();

                return new JobDetailsResponseDto
                {
                    Success = true,
                    Message = request.JobId.HasValue && request.JobId != Guid.Empty
                        ? "Job details updated successfully."
                        : "Job details saved as draft.",

                    JobId = job.JobId,
                    JobStatus = job.JobStatus.ToString(),
                    StepStatus = BuildStepStatus(job)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveJobDetailsAsync error.");

                return new JobDetailsResponseDto
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }


        public async Task<BaseJobResponseDto> SaveCompensationAsync(
           CompensationRequestDto request,
           Guid jobId,
           Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(
                    jobId,
                    employerId);

                // =====================================================
                // CREATE IF NOT EXISTS
                // =====================================================

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = jobId == Guid.Empty
                            ? Guid.NewGuid()
                            : jobId,

                        EmployerId = employerId,

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 2,
                        LastCompletedStep = 2,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,

                        SalaryCurrency = job.SalaryCurrency,
                        SalaryDisplayOption = "Show Range"
                    };

                    _context.JobPostings.Add(job);
                }

                // =====================================================
                // VALIDATION
                // =====================================================

                if (request.SalaryMin.HasValue &&
                    request.SalaryMax.HasValue &&
                    request.SalaryMin.Value > request.SalaryMax.Value)
                {
                    return Fail(
                        "Min salary cannot be greater than max salary.");
                }

                // =====================================================
                // PATCH VALUES
                // =====================================================

                if (request.SalaryMin.HasValue)
                {
                    job.SalaryMin = request.SalaryMin.Value;
                }

                if (request.SalaryMax.HasValue)
                {
                    job.SalaryMax = request.SalaryMax.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.SalaryCurrency))
                {
                    job.SalaryCurrency = request.SalaryCurrency.Trim().ToUpper();
                }

                if (!string.IsNullOrWhiteSpace(request.SalaryDisplayOption))
                {
                    job.SalaryDisplayOption = request.SalaryDisplayOption;
                }

                // =====================================================
                // STEP TRACKING
                // =====================================================

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 2);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 2);

                job.UpdatedAt =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(
                    job,
                    "Compensation saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SaveCompensationAsync error.");

                return Fail(
                    ex.InnerException?.Message ??
                    ex.Message);
            }
        }
        // ════════════════════════════════════════════════
        // STEP 3 — Skills & JD
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveSkillsAsync(
            SkillsRequestDto request,
            Guid jobId,
            Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = jobId == Guid.Empty
                            ? Guid.NewGuid()
                            : jobId,

                        EmployerId = employerId,

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 3,

                        LastCompletedStep = 3,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }

                // PATCH logic

                if (request.KeySkills != null)
                {
                    job.KeySkills = request.KeySkills;
                }

                if (!string.IsNullOrWhiteSpace(request.LicenceDocsRequired))
                {
                    job.LicenceDocsRequired = request.LicenceDocsRequired;
                }

                if (!string.IsNullOrWhiteSpace(request.LanguageRequired))
                {
                    job.LanguageRequired = request.LanguageRequired;
                }

                if (request.Benefits != null)
                {
                    job.Benefits = request.Benefits;
                }

                if (request.Tags != null)
                {
                    job.Tags = request.Tags;
                }



                job.CurrentStep =
                    Math.Max(job.CurrentStep, 3);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 3);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(job, "Skills saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save skills error.");

                return Fail(
                    ex.InnerException?.Message ??
                    ex.Message);
            }
        }

        // ════════════════════════════════════════════════
        // STEP 4 — Eligibility
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveEligibilityAsync(
        EligibilityRequestDto request,
        Guid jobId,
        Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = jobId == Guid.Empty
                            ? Guid.NewGuid()
                            : jobId,

                        EmployerId = employerId,

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 4,
                        LastCompletedStep = 4,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }

                // Validation

                if (request.AgeMin.HasValue &&
                    request.AgeMax.HasValue &&
                    request.AgeMin > request.AgeMax)
                {
                    return Fail(
                        "Min age cannot be greater than max age.");
                }

                if (request.PassportRequired == true &&
                    !request.PassportValidityMonths.HasValue)
                {
                    return Fail(
                        "Passport validity months required when passport is required.");
                }

                // Vacancies

                if (request.Vacancies.HasValue)
                {
                    job.Vacancies = (short)request.Vacancies.Value;
                }

                // Education

                if (!string.IsNullOrWhiteSpace(request.EducationRequired))
                {
                    job.EducationRequired = request.EducationRequired.Trim();
                }

                // Age

                if (request.AgeMin.HasValue)
                {
                    job.AgeMin =
                        (byte)request.AgeMin.Value;
                }

                if (request.AgeMax.HasValue)
                {
                    job.AgeMax =
                        (byte)request.AgeMax.Value;
                }

                // Gender

                if (request.GenderPreferred.HasValue)
                {
                    job.GenderPreferred =
                        request.GenderPreferred.Value;
                }

                // Disability

                if (request.DisabilityEligible.HasValue)
                {
                    job.DisabilityEligible =
                        request.DisabilityEligible.Value;
                }

                // Passport

                if (request.PassportRequired.HasValue)
                {
                    job.PassportRequired =
                        request.PassportRequired.Value;
                }

                if (request.PassportValidityMonths.HasValue)
                {
                    job.PassportValidityMonths =
                        (byte)request.PassportValidityMonths.Value;
                }

                // Step tracking

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 4);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 4);

                job.UpdatedAt =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(
                    job,
                    "Eligibility saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SaveEligibilityAsync error.");

                return Fail(
                    ex.InnerException?.Message ??
                    ex.Message);
            }
        }
        // ════════════════════════════════════════════════
        // STEP 5 — Location
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveLocationAsync(
         LocationRequestDto request,
         Guid jobId,
         Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = jobId == Guid.Empty
                            ? Guid.NewGuid()
                            : jobId,

                        EmployerId = employerId,

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 5,
                        LastCompletedStep = 5,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }

                // ==================================================
                // Validation
                // ==================================================

                if (!request.LocationType.HasValue)
                    return Fail("Location type is required.");

                if (request.LocationType == LocationType.Onshore)
                {
                    if (string.IsNullOrWhiteSpace(request.OnshoreCity))
                        return Fail("City is required for onshore jobs.");

                    if (string.IsNullOrWhiteSpace(request.OnshoreState))
                        return Fail("State is required for onshore jobs.");

                    if (string.IsNullOrWhiteSpace(request.OnshoreCountry))
                        return Fail("Country is required for onshore jobs.");
                }

                if (request.LocationType == LocationType.Offshore)
                {
                    if (string.IsNullOrWhiteSpace(request.OffshoreRegion))
                        return Fail("Offshore region is required.");

                    if (string.IsNullOrWhiteSpace(request.OffshoreCountry))
                        return Fail("Country is required for offshore jobs.");
                }

                // ==================================================
                // Save Location Type
                // ==================================================

                job.LocationType = request.LocationType.Value;

                // ==================================================
                // Onshore
                // ==================================================

                if (request.LocationType == LocationType.Onshore)
                {
                    job.WorkAddressLine = request.WorkAddressLine?.Trim();
                    job.OnshoreCity = request.OnshoreCity?.Trim();
                    job.OnshoreState = request.OnshoreState?.Trim();
                    job.OnshoreCountry = request.OnshoreCountry?.Trim();
                    job.OnshorePincode = request.OnshorePincode?.Trim();

                    // Clear offshore fields
                    job.OffshoreVesselName = null;
                    job.OffshoreRegion = null;
                    job.OffshoreCountry = null;

                    job.IsInternational =
                        !string.Equals(
                            job.OnshoreCountry,
                            "India",
                            StringComparison.OrdinalIgnoreCase);
                }

                // ==================================================
                // Offshore
                // ==================================================

                if (request.LocationType == LocationType.Offshore)
                {
                    job.OffshoreVesselName = request.OffshoreVesselName?.Trim();
                    job.OffshoreRegion = request.OffshoreRegion?.Trim();
                    job.OffshoreCountry = request.OffshoreCountry?.Trim();

                    // Clear onshore fields
                    job.WorkAddressLine = null;
                    job.OnshoreCity = null;
                    job.OnshoreState = null;
                    job.OnshoreCountry = null;
                    job.OnshorePincode = null;

                    job.IsInternational =
                        !string.Equals(
                            job.OffshoreCountry,
                            "India",
                            StringComparison.OrdinalIgnoreCase);
                }

                // ==================================================
                // Step Tracking
                // ==================================================

                job.CurrentStep = Math.Max(job.CurrentStep, 5);
                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 5);
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(
                    job,
                    "Location saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SaveLocationAsync error.");

                return Fail(
                    ex.InnerException?.Message ??
                    ex.Message);
            }
        }
        // ════════════════════════════════════════════════
        // STEP 6 — Screening Questions
        // ════════════════════════════════════════════════
        public async Task<BaseJobResponseDto> SaveQuestionsAsync(
       QuestionsRequestDto request,
       Guid jobId,
       Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                if (job == null)
                {
                    job = new JobPosting
                    {
                        JobId = jobId == Guid.Empty
                            ? Guid.NewGuid()
                            : jobId,

                        EmployerId = employerId,

                        JobStatus = JobStatus.Draft,

                        CurrentStep = 6,
                        LastCompletedStep = 6,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }

                // =====================================================
                // Validate Questions
                // =====================================================

                var questions = request.Questions?
                    .Where(x => !string.IsNullOrWhiteSpace(x.QuestionText))
                    .Select(x => x.QuestionText.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                if (questions.Count > 5)
                {
                    return Fail("Maximum 5 screening questions allowed.");
                }

                if (request.Questions != null && questions.Count == 0)
                {
                    return Fail("Please add at least one valid screening question.");
                }

                // =====================================================
                // Save Questions
                // =====================================================

                job.ScreeningQuestions = questions;

                // =====================================================
                // Update Wizard Progress
                // =====================================================

                job.CurrentStep = Math.Max(job.CurrentStep, 6);

                job.LastCompletedStep = Math.Max(job.LastCompletedStep, 6);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(
                    job,
                    "Questions saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SaveQuestionsAsync failed for JobId {JobId}",
                    jobId);

                return Fail(
                    ex.InnerException?.Message ??
                    ex.Message);
            }
        }
        // ════════════════════════════════════════════════
        // STEP 7 — Publish or Save Draft
        // ════════════════════════════════════════════════
        public async Task<PublishingResponseDto> PublishJobAsync(
            PublishingRequestDto request,
            Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(request.JobId, employerId);

                if (job == null)
                {
                    return new PublishingResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };
                }

                // Validate required steps only when trying to publish
                if (request.PublishNow == true &&
                    job.LastCompletedStep < 5)
                {
                    return new PublishingResponseDto
                    {
                        Success = false,
                        Message =
                            $"Please complete all required steps. Last completed: Step {job.LastCompletedStep} ({StepNames[job.LastCompletedStep]})."
                    };
                }

                // PATCH logic

                if (request.ApplicationDeadline.HasValue)
                {
                    job.ApplicationDeadline =
                        request.ApplicationDeadline.Value;
                }

                if (request.CompanyVisibility.HasValue)
                {
                    job.CompanyVisibility =
                        request.CompanyVisibility.Value;
                }

             

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 7);

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 7);

                job.UpdatedAt = DateTime.UtcNow;

                // Publish / Unpublish only if requested

                if (request.PublishNow.HasValue)
                {
                    if (request.PublishNow.Value)
                    {
                        job.JobStatus = JobStatus.Active;

                        if (!job.PublishedAt.HasValue)
                        {
                            job.PublishedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        job.JobStatus = JobStatus.Draft;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job updated — JobId:{JobId} Employer:{EmployerId}",
                    job.JobId,
                    employerId);


                if (job.JobStatus == JobStatus.Active)
                {
                    try
                    {
                        await _embeddingStorage.GenerateJobEmbeddingAsync(job.JobId);
                    }
                    catch (Exception embedEx)
                    {
                        _logger.LogWarning(
                            embedEx,
                            "Embedding generation failed for JobId:{JobId} — publish still succeeded.",
                            job.JobId);
                    }
                }

                return new PublishingResponseDto
                {
                    Success = true,
                    Message = request.PublishNow == true
                        ? "Job published successfully!"
                        : "Publishing settings saved.",

                    JobStatus = job.JobStatus.ToString(),
                    PublishedAt = job.PublishedAt,
                    JobUrl = job.JobStatus == JobStatus.Active
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
                    Message =
                        ex.InnerException?.Message ??
                        ex.Message
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

                job.JobStatus = JobStatus.Draft;
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
     Guid jobId,
     Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                if (job == null)
                {
                    return new ResumeJobResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };
                }

                return new ResumeJobResponseDto
                {
                    Success = true,

                    Message =
                        $"Resume from Step {job.LastCompletedStep + 1} " +
                        $"({StepNames.GetValueOrDefault(job.LastCompletedStep + 1, "Publishing")}).",

                    JobId = job.JobId,

                    StepStatus = BuildStepStatus(job),

                    // =====================================================
                    // STEP 1 - JOB DETAILS
                    // =====================================================

                    Step1Data = new JobDetailsRequestDto
                    {
                        JobTitle = job.JobTitle,
                        TradeCategory = job.TradeCategory,
                        Role = job.Role,

                        ExperienceMinYears = job.ExperienceMinYears,
                        ExperienceMaxYears = job.ExperienceMaxYears,
                        IndustryType = job.IndustryType,
                        JobDescription = job.JobDescription,

                        EmploymentType = job.EmploymentType,
                        EmploymentMode = job.EmploymentMode,
                        Department = job.Department,

                        DutyHoursPerDay = job.DutyHoursPerDay,
                        PaidOvertime = job.PaidOvertime,

                        KeyResponsibilities =
                            job.KeyResponsibilities ?? new List<string>()
                    },

                    // =====================================================
                    // STEP 2 - COMPENSATION
                    // =====================================================

                    Step2Data = new CompensationRequestDto
                    {
                        SalaryMin = job.SalaryMin,
                        SalaryMax = job.SalaryMax,

                        SalaryCurrency =
                            job.SalaryCurrency,

                        SalaryDisplayOption =
                            job.SalaryDisplayOption
                    },

                    // =====================================================
                    // STEP 3 - SKILLS
                    // =====================================================

                    Step3Data = new SkillsRequestDto
                    {
                        KeySkills =
                            job.KeySkills ?? new List<string>(),

                        LicenceDocsRequired =
                            job.LicenceDocsRequired,

                        LanguageRequired =
                            job.LanguageRequired,

                        Benefits =
                            job.Benefits ?? new List<string>(),

                        Tags =
                            job.Tags ?? new List<string>()
                    },

                    // =====================================================
                    // STEP 4 - ELIGIBILITY
                    // =====================================================

                    Step4Data = new EligibilityRequestDto
                    {
                        Vacancies = job.Vacancies,

                        EducationRequired = job.EducationRequired,

                        AgeMin = job.AgeMin,
                        AgeMax = job.AgeMax,

                        GenderPreferred =
                            job.GenderPreferred,

                        DisabilityEligible =
                            job.DisabilityEligible,

                        PassportRequired =
                            job.PassportRequired,

                        PassportValidityMonths =
                            job.PassportValidityMonths
                    },

                    // =====================================================
                    // STEP 5 - LOCATION
                    // =====================================================

                    Step5Data = new LocationRequestDto
                    {
                        LocationType =
                            job.LocationType,

                        WorkAddressLine =
                            job.WorkAddressLine,

                        OnshoreCity =
                            job.OnshoreCity,

                        OnshoreState =
                            job.OnshoreState,

                        OnshoreCountry =
                            job.OnshoreCountry,

                        OnshorePincode =
                            job.OnshorePincode,

                        OffshoreVesselName =
                            job.OffshoreVesselName,

                        OffshoreRegion =
                            job.OffshoreRegion,

                        OffshoreCountry =
                            job.OffshoreCountry
                    },

                    // =====================================================
                    // STEP 6 - QUESTIONS
                    // =====================================================

                    Step6Data = new QuestionsRequestDto
                    {
                        Questions =
                            job.ScreeningQuestions?
                                .Select(q => new ScreeningQuestion
                                {
                                    QuestionText = q
                                })
                                .ToList()
                            ?? new List<ScreeningQuestion>()
                    },
                    Step7Data = new PublishingRequestDto
                    {
                        JobId = job.JobId,

                        ApplicationDeadline =
        job.ApplicationDeadline,

                        CompanyVisibility =
        job.CompanyVisibility,

                        PublishNow =
        job.JobStatus == JobStatus.Active
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ResumeJobAsync error.");

                return new ResumeJobResponseDto
                {
                    Success = false,
                    Message =
                        ex.InnerException?.Message ??
                        ex.Message
                };
            }
        }

        public async Task<BaseJobResponseDto> DeleteJobAsync(
           Guid jobId,
           Guid employerId)
        {
            try
            {
                var job = await _context.JobPostings
                    .FirstOrDefaultAsync(x =>
                        x.JobId == jobId &&
                        x.EmployerId == employerId &&
                        !x.IsDeleted);

                if (job == null)
                {
                    return Fail("Job not found.");
                }

                job.IsDeleted = true;
                job.IsActive = false;
                job.JobStatus = JobStatus.Archived;
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job deleted. JobId: {JobId}, EmployerId: {EmployerId}",
                    jobId,
                    employerId);

                return new BaseJobResponseDto
                {
                    Success = true,
                    Message = "Job deleted successfully.",
                    JobId = job.JobId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());

                return new ResumeJobResponseDto
                {
                    Success = false,
                    Message = ex.ToString()
                };
            }
        }

        // ── Private Helpers ───────────────────────────────────
        private async Task<JobPosting?> GetJobAsync(
        Guid jobId,
        Guid employerId) =>
        await _context.JobPostings
            .FirstOrDefaultAsync(j =>
                j.JobId == jobId &&
                j.EmployerId == employerId &&
                !j.IsDeleted &&
                j.JobStatus != JobStatus.Archived);

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
                JobStatus = job.JobStatus.ToString(),
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
