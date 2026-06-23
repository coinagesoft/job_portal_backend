    using global::JobPortal.Application.DTOs.Recruiter;
    using global::JobPortal.Domain.Entities;
    using global::JobPortal.Infrastructure.Persistence;
    using global::JobPortal.Services.IImplement.IRecruiter;
    using JobPortal.Application.DTOs.JobPosting;
    using JobPortal.Domain.Enums.common;
    using JobPortal.Domain.Enums.RecruiterEnums;
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
       JobDetailsRequestDto request, Guid employerId)
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

                // CREATE
                if (!request.JobId.HasValue ||
                    request.JobId.Value == Guid.Empty)
                {
                    job = new JobPosting
                    {
                        JobId = Guid.NewGuid(),
                        EmployerId = employerId,
                        JobTitle = request.JobTitle ?? string.Empty,
                        TradeCategory = request.TradeCategory ?? string.Empty,
                        Role = request.Role ?? string.Empty,
                        ExperienceRequiredYears =
                            (byte)(request.ExperienceRequiredYears ?? 0),
                        JobDescription = request.JobDescription ?? string.Empty,

                        JobStatus = JobStatus.Draft,
                        CurrentStep = 1,
                        LastCompletedStep = 1,

                        ApplicationDeadline =
                            DateOnly.FromDateTime(
                                DateTime.UtcNow.AddDays(30)),

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }
                else
                {
                    // UPDATE
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

}

                await _context.SaveChangesAsync();

        return new JobDetailsResponseDto
        {
            Success = true,
            Message = request.JobId.HasValue
                ? "Job details updated successfully."
                : "Job details saved as draft.",

            JobId = job.JobId,
            JobStatus = job.JobStatus.ToString(),
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
        

        public async Task<BaseJobResponseDto> SaveCompensationAsync(
        CompensationRequestDto request,
        Guid jobId,
        Guid employerId)
        {
            try
            {
                var job = await GetJobAsync(jobId, employerId);

                // CREATE IF NOT EXISTS
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

                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.JobPostings.Add(job);
                }

                // VALIDATE ONLY IF BOTH PROVIDED
                if (request.SalaryMin.HasValue &&
                    request.SalaryMax.HasValue &&
                    request.SalaryMin > request.SalaryMax)
                {
                    return Fail(
                        "Min salary cannot be greater than max salary.");
                }

                // PATCH LOGIC

                if (request.SalaryMin.HasValue)
                    job.SalaryMin = request.SalaryMin.Value;

                if (request.SalaryMax.HasValue)
                    job.SalaryMax = request.SalaryMax.Value;

                if (request.SalaryCurrency.HasValue)
                    job.SalaryCurrency =
                        request.SalaryCurrency.Value.ToString();

                if (request.SalaryDisplayOption.HasValue)
                    job.SalaryDisplayOption =
                        request.SalaryDisplayOption.Value.ToString();

                job.CurrentStep = Math.Max(job.CurrentStep, 2);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 2);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(
                    job,
                    "Compensation saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save compensation error.");

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
                    job.KeySkills =
                        JsonSerializer.Serialize(request.KeySkills);
                }

                if (!string.IsNullOrWhiteSpace(
                    request.LicenceDocsRequired))
                {
                    job.LicenceDocsRequired =
                        request.LicenceDocsRequired;
                }

                if (!string.IsNullOrWhiteSpace(
                    request.LanguageRequired))
                {
                    job.LanguageRequired =
                        request.LanguageRequired;
                }

                if (!string.IsNullOrWhiteSpace(
                    request.AdditionalJobDescription))
                {
                    job.JobDescription =
                        string.IsNullOrWhiteSpace(job.JobDescription)
                            ? request.AdditionalJobDescription
                            : job.JobDescription +
                              "\n\n" +
                              request.AdditionalJobDescription;
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

                // PATCH logic

                if (request.Vacancies.HasValue)
                {
                    job.Vacancies = (short)request.Vacancies.Value;
                }

                if (request.EducationRequired.HasValue)
                    job.EducationRequired =
                        request.EducationRequired.Value.ToString();

                if (request.AgeMin.HasValue)
                    job.AgeMin =
                        (byte)request.AgeMin.Value;

                if (request.AgeMax.HasValue)
                    job.AgeMax =
                        (byte)request.AgeMax.Value;

                if (request.GenderPreferred.HasValue)
                    job.GenderPreferred =
                        request.GenderPreferred.Value.ToString();

                if (request.DisabilityEligible.HasValue)
                    job.DisabilityEligible =
                        request.DisabilityEligible.Value;

                if (request.PassportRequired.HasValue)
                    job.PassportRequired =
                        request.PassportRequired.Value;

                if (request.PassportValidityMonths.HasValue)
                    job.PassportValidityMonths =
                        (byte)request.PassportValidityMonths.Value;

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 4);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 4);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(job, "Eligibility saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save eligibility error.");

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

                // Validate only when LocationType supplied

                if (request.LocationType.HasValue)
                {
                    if (request.LocationType == LocationType.Onshore)
                    {
                        if (request.OnshoreCity != null &&
                            string.IsNullOrWhiteSpace(request.OnshoreCity))
                            return Fail("City is required for onshore jobs.");

                        if (request.OnshoreState != null &&
                            string.IsNullOrWhiteSpace(request.OnshoreState))
                            return Fail("State is required for onshore jobs.");
                    }
                    else
                    {
                        if (request.OffshoreRegion != null &&
                            string.IsNullOrWhiteSpace(request.OffshoreRegion))
                            return Fail("Offshore region is required.");
                    }
                }

                // PATCH

                if (request.LocationType.HasValue)
                    job.LocationType =
                        request.LocationType.Value.ToString();

                if (request.OnshoreCity != null)
                    job.OnshoreCity = request.OnshoreCity;

                if (request.OnshoreState != null)
                    job.OnshoreState = request.OnshoreState;

                if (request.OffshoreVesselName != null)
                    job.OffshoreVesselName =
                        request.OffshoreVesselName;

                if (request.OffshoreRegion != null)
                    job.OffshoreRegion =
                        request.OffshoreRegion;

                if (request.LocationType.HasValue)
                {
                    job.IsInternational =
                        request.LocationType == LocationType.Offshore
                        || job.PassportRequired;
                }

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 5);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 5);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(job, "Location saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save location error.");

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

                if (request.Questions != null)
                {
                    if (request.Questions.Count > 5)
                    {
                        return Fail(
                            "Maximum 5 screening questions allowed.");
                    }

                    job.ScreeningQuestions =
                        JsonSerializer.Serialize(
                            request.Questions);
                }

                job.CurrentStep =
                    Math.Max(job.CurrentStep, 6);

                job.LastCompletedStep =
                    Math.Max(job.LastCompletedStep, 6);

                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(job, "Questions saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save questions error.");

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
                        request.CompanyVisibility.Value.ToString();
                }

                if (request.PublishingTags != null)
                {
                    job.PublishingTags =
                        JsonSerializer.Serialize(
                            request.PublishingTags);
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

                return new PublishingResponseDto
                {
                    Success = true,
                    Message = request.PublishNow == true
                        ? "Job published successfully!"
                        : "Publishing settings saved.",

                    JobId = job.JobId,
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
            Guid jobId, Guid employerId)
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
                    Message = $"Resume from Step {job.LastCompletedStep + 1} ({StepNames.GetValueOrDefault(job.LastCompletedStep + 1, "Publishing")}).",
                    JobId = job.JobId,
                    StepStatus = BuildStepStatus(job),

                    // STEP 1 - Job Details
                    Step1Data = new JobDetailsRequestDto
                    {
                        JobTitle = job.JobTitle,
                        TradeCategory = job.TradeCategory,
                        Role = job.Role,
                        ExperienceRequiredYears = job.ExperienceRequiredYears,
                        JobDescription = job.JobDescription
                    },

                    // STEP 2 - Compensation
                    Step2Data = new CompensationRequestDto
                    {
                        SalaryMin = job.SalaryMin,
                        SalaryMax = job.SalaryMax,

                        SalaryCurrency =
                            Enum.TryParse<SalaryCurrency>(
                                job.SalaryCurrency,
                                true,
                                out var currency)
                                    ? currency
                                    : SalaryCurrency.INR,

                        SalaryDisplayOption =
                            Enum.TryParse<SalaryDisplayOption>(
                                job.SalaryDisplayOption,
                                true,
                                out var display)
                                    ? display
                                    : SalaryDisplayOption.Show_Range
                    },

                    // STEP 3 - Skills
                    Step3Data = new SkillsRequestDto
                    {
                        KeySkills =
                            string.IsNullOrWhiteSpace(job.KeySkills)
                                ? new List<string>()
                                : JsonSerializer.Deserialize<List<string>>(job.KeySkills) ?? new List<string>(),

                        LicenceDocsRequired = job.LicenceDocsRequired,
                        LanguageRequired = job.LanguageRequired
                    },

                    // STEP 4 - Eligibility
                    Step4Data = new EligibilityRequestDto
                    {
                        Vacancies = job.Vacancies,

                        EducationRequired =
                            Enum.TryParse<EducationLevel>(
                                job.EducationRequired,
                                true,
                                out var education)
                                    ? education
                                    : default,

                        AgeMin = job.AgeMin,
                        AgeMax = job.AgeMax,

                        GenderPreferred =
                            Enum.TryParse<GenderPreferred>(
                                job.GenderPreferred,
                                true,
                                out var gender)
                                    ? gender
                                    : GenderPreferred.Any,

                        DisabilityEligible = job.DisabilityEligible,
                        PassportRequired = job.PassportRequired,
                        PassportValidityMonths = job.PassportValidityMonths
                    },

                    // STEP 5 - Location
                    Step5Data = new LocationRequestDto
                    {
                        LocationType =
                            Enum.TryParse<LocationType>(
                                job.LocationType,
                                true,
                                out var locationType)
                                    ? locationType
                                    : default,

                        OnshoreCity = job.OnshoreCity,
                        OnshoreState = job.OnshoreState,
                        OffshoreVesselName = job.OffshoreVesselName,
                        OffshoreRegion = job.OffshoreRegion,
                        Country = "India"
                    },

                    // STEP 6 - Questions
                    Step6Data = new QuestionsRequestDto
                    {
                        Questions =
                            string.IsNullOrWhiteSpace(job.ScreeningQuestions)
                                ? new List<ScreeningQuestion>()
                                : JsonSerializer.Deserialize<List<ScreeningQuestion>>(job.ScreeningQuestions)
                                    ?? new List<ScreeningQuestion>()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resume job error.");

                return new ResumeJobResponseDto
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        // ── Private Helpers ───────────────────────────────────
        private async Task<JobPosting?> GetJobAsync(Guid jobId, Guid employerId) =>
      await _context.JobPostings
          .FirstOrDefaultAsync(j =>
              j.JobId == jobId &&
              j.EmployerId == employerId &&
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
