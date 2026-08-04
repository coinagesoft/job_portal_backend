using JobPortal.Application.DTOs.Recruiter.Applicants;
using JobPortal.Application.DTOs.Recruiter.JobListing;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterApplicantService : IRecruiterApplicantService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterApplicantService> _logger;

        public RecruiterApplicantService(
            AppDbContext context,
            ILogger<RecruiterApplicantService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================================
        // Dashboard
        // ==========================================================
        public async Task<ApplicantDashboardResponseDto> GetDashboardAsync(
            Guid employerId)
        {
            var applications = await _context.JobApplications
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .ToListAsync();

            return new ApplicantDashboardResponseDto
            {
                TotalApplicants = applications.Count,

                Applied = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Applied),

                InReview = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.InReview),

                Shortlisted = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Shortlisted),

                Interview = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Interview),

                TableInterview = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.TableInterview),

                CvSelection = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.CvSelection),

                LocationInterview = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.LocationInterview),

                Rejected = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Rejected),

                Hired = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Hired),

                Withdrawn = applications.Count(x =>
                    x.ApplicationStatus == ApplicationStatus.Withdrawn)
            };
        }

        // Notice period is stored as free text ("Immediate", "30 Days", "60
        // Days", …). Returns the number of days, or null if it can't be
        // parsed — an unparseable/empty value never matches a "<= N days"
        // filter rather than guessing.
        private static int? NoticePeriodDaysOrNull(string? noticePeriod)
        {
            if (string.IsNullOrWhiteSpace(noticePeriod)) return null;

            if (noticePeriod.Trim().Equals("Immediate", StringComparison.OrdinalIgnoreCase))
                return 0;

            var digits = new string(noticePeriod.TakeWhile(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                // Handle values like "Notice: 30 Days" where the number
                // isn't at the very start.
                var match = System.Text.RegularExpressions.Regex.Match(noticePeriod, @"\d+");
                digits = match.Success ? match.Value : "";
            }

            return int.TryParse(digits, out var days) ? days : null;
        }

        // ==========================================================
        // Applicant List
        // ==========================================================
        public async Task<ApplicantListResponseDto> GetApplicantsAsync(
            Guid employerId,
            ApplicantListRequestDto request)
        {
            var query = _context.JobApplications
                .AsNoTracking()
                .Include(x => x.CandidateProfile)
                .Include(x => x.JobPosting)
                .Where(x => x.EmployerId == employerId)
                .AsQueryable();

            // Job Filter
            if (request.JobId.HasValue)
            {
                query = query.Where(x =>
                    x.JobId == request.JobId.Value);
            }

            // Status Filter
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<ApplicationStatus>(
                    request.Status,
                    true,
                    out var status))
                {
                    query = query.Where(x =>
                        x.ApplicationStatus == status);
                }
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();

                query = query.Where(x =>
                    x.CandidateProfile.FullName.ToLower().Contains(search)
                    ||
                    x.JobPosting.JobTitle.ToLower().Contains(search)
                    ||
                    (x.CandidateProfile.PrimaryTrade != null &&
                     x.CandidateProfile.PrimaryTrade.ToLower().Contains(search))
                    ||
                    (x.CandidateProfile.CurrentCity != null &&
                     x.CandidateProfile.CurrentCity.ToLower().Contains(search)));
            }

            // Experience 3+ years — plain numeric comparison, translates
            // fine to SQL.
            if (request.MinExperience3Years == true)
            {
                query = query.Where(x => x.CandidateProfile.TotalExperienceYears >= 3);
            }

            var needsInMemoryFilter =
                request.NoticePeriodMax30Days == true ||
                request.MandatoryAnswersComplete == true;

            List<JobApplication> applications;
            int totalRecords;

            if (needsInMemoryFilter)
            {
                // Notice period is free text ("30 Days", "Immediate", …) and
                // "mandatory answers complete" compares two list lengths —
                // neither translates to SQL, so filter in memory instead.
                var all = await query
                    .OrderByDescending(x => x.AppliedAt)
                    .ToListAsync();

                if (request.NoticePeriodMax30Days == true)
                {
                    all = all
                        .Where(x => NoticePeriodDaysOrNull(x.CandidateProfile.NoticePeriod) is int days && days <= 30)
                        .ToList();
                }

                if (request.MandatoryAnswersComplete == true)
                {
                    all = all
                        .Where(x =>
                        {
                            var required = x.JobPosting.ScreeningQuestions?.Count ?? 0;
                            if (required == 0) return true;
                            var answered = x.ScreeningAnswers?.Count(a => !string.IsNullOrWhiteSpace(a)) ?? 0;
                            return answered >= required;
                        })
                        .ToList();
                }

                totalRecords = all.Count;
                applications = all
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();
            }
            else
            {
                totalRecords = await query.CountAsync();

                applications = await query
                    .OrderByDescending(x => x.AppliedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();
            }

            var candidateIds = applications
                .Select(x => x.CandidateId)
                .Distinct()
                .ToList();

            var unlockedCandidates =
                await _context.EmployerCandidateAccesses
                .Where(x =>
                    x.EmployerId == employerId &&
                    x.IsActive)
                .Select(x => x.CandidateId)
                .ToListAsync();

            var downloadedCandidates =
                await _context.CandidateCvDownloads
                .Where(x => x.EmployerId == employerId)
                .Select(x => x.CandidateId)
                .Distinct()
                .ToListAsync();

            var items = applications
                .Select(x => new ApplicantListItemDto
                {
                    ApplicationId = x.ApplicationId,

                    CandidateId = x.CandidateId,

                    JobId = x.JobId,

                    CandidateName =
                        x.CandidateProfile.FullName,

                    JobTitle =
                        x.JobPosting.JobTitle,

                    PrimaryTrade =
                        x.CandidateProfile.PrimaryTrade,

                    ExperienceYears =
                        x.CandidateProfile.TotalExperienceYears,

                    CurrentCity =
                        x.CandidateProfile.CurrentCity,

                    CurrentState =
                        x.CandidateProfile.CurrentState,

                    ApplicationStatus =
                        x.ApplicationStatus.ToString(),

                    AppliedAt =
                        x.AppliedAt,

                    IsShortlisted =
                        x.IsShortlisted,

                    IsUnlocked =
                        unlockedCandidates.Contains(
                            x.CandidateId),

                    CvDownloaded =
                        downloadedCandidates.Contains(
                            x.CandidateId)
                })
                .ToList();

            return new ApplicantListResponseDto
            {
                TotalRecords = totalRecords,

                PageNumber = request.PageNumber,

                PageSize = request.PageSize,

                Applicants = items
            };
        }

        // ==========================================================
        // TODO (Part 2)
        // ==========================================================
        public async Task<ApplicantDetailsResponseDto?> GetApplicantDetailsAsync(
      Guid employerId,
      Guid applicationId)
        {
            var application = await _context.JobApplications
                .Include(x => x.CandidateProfile)
                    .ThenInclude(x => x.Educations)
                .Include(x => x.CandidateProfile)
                    .ThenInclude(x => x.WorkHistories)
                .Include(x => x.CandidateProfile)
                    .ThenInclude(x => x.Skills)
                .Include(x => x.CandidateProfile)
                    .ThenInclude(x => x.Cvs)
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return null;
            }

            if (application.ViewedAt == null)
            {
                application.ViewedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var candidate = application.CandidateProfile;

            return new ApplicantDetailsResponseDto
            {
                ApplicationId = application.ApplicationId,

                CandidateId = candidate.CandidateId,

                JobId = application.JobId,

                CandidateName = candidate.FullName,

                ProfilePhotoUrl = candidate.ProfilePhotoUrl,

                PrimaryTrade = candidate.PrimaryTrade,

                TotalExperienceYears =
                    candidate.TotalExperienceYears,

                CurrentCity =
                    candidate.CurrentCity,

                CurrentState =
                    candidate.CurrentState,

                ProfessionalSummary =
                    candidate.ProfessionalSummary,

                About =
                    candidate.About,

                ApplicationStatus =
                    application.ApplicationStatus.ToString(),

                IsShortlisted =
                    application.IsShortlisted,

                AppliedAt =
                    application.AppliedAt,

                ViewedAt =
                    application.ViewedAt,

                Educations =
                    candidate.Educations
                    .Select(x =>
                        new ApplicantEducationDto
                        {
                            EducationLevel =
                                x.EducationLevel,

                            InstituteName =
                                x.InstituteName,

                            PassoutYear =
                                x.PassoutYear,

                            CertificateUrl =
                                x.CertificateUrl
                        })
                    .ToList(),

                WorkHistories =
                    candidate.WorkHistories
                    .Select(x =>
                        new ApplicantWorkHistoryDto
                        {
                            CompanyName =
                                x.CompanyName,

                            JobTitle =
                                x.JobTitle,

                            StartDate =
                                x.StartDate,

                            EndDate =
                                x.EndDate,

                            IsCurrent =
                                x.IsCurrent,

                            WorkLocation =
                                x.WorkLocation,

                            IsOffshore =
                                x.IsOffshore
                        })
                    .ToList(),

                Skills =
                    candidate.Skills
                    .Select(x =>
                        new ApplicantSkillDto
                        {
                            SkillName =
                                x.SkillName,

                            SkillType =
                                x.SkillType,

                            YearsOfExperience =
                                x.YearsOfExperience
                        })
                    .ToList(),

                Cvs =
                    candidate.Cvs
                    .Select(x =>
                        new ApplicantCvDto
                        {
                            CvId =
                                x.CvId,

                            CvFileUrl =
                                x.CvFileUrl,

                            CvPdfUrl =
                                x.CvPdfUrl,

                            GeneratedAt =
                                x.GeneratedAt
                        })
                    .ToList()
            };
        }

        public async Task<JobApplicantsResponseDto?> GetJobApplicantsAsync(
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

            var applications = await _context.JobApplications
                .AsNoTracking()
                .Include(x => x.CandidateProfile)
                .Where(x =>
                    x.JobId == jobId &&
                    x.EmployerId == employerId)
                .OrderByDescending(x => x.AppliedAt)
                .ToListAsync();

            var unlockedCandidates =
                await _context.EmployerCandidateAccesses
                .Where(x =>
                    x.EmployerId == employerId &&
                    x.IsActive)
                .Select(x => x.CandidateId)
                .ToListAsync();

            var downloadedCandidates =
                await _context.CandidateCvDownloads
                .Where(x =>
                    x.EmployerId == employerId)
                .Select(x => x.CandidateId)
                .Distinct()
                .ToListAsync();

            return new JobApplicantsResponseDto
            {
                JobId = job.JobId,

                JobTitle = job.JobTitle,

                TotalApplicants =
                    applications.Count,

                Applicants =
                    applications
                    .Select(x =>
                        new ApplicantListItemDto
                        {
                            ApplicationId =
                                x.ApplicationId,

                            CandidateId =
                                x.CandidateId,

                            JobId =
                                x.JobId,

                            CandidateName =
                                x.CandidateProfile.FullName,

                            JobTitle =
                                job.JobTitle,

                            PrimaryTrade =
                                x.CandidateProfile.PrimaryTrade,

                            ExperienceYears =
                                x.CandidateProfile
                                 .TotalExperienceYears,

                            CurrentCity =
                                x.CandidateProfile.CurrentCity,

                            CurrentState =
                                x.CandidateProfile.CurrentState,

                            ApplicationStatus =
                                x.ApplicationStatus.ToString(),

                            AppliedAt =
                                x.AppliedAt,

                            IsShortlisted =
                                x.IsShortlisted,

                            IsUnlocked =
                                unlockedCandidates.Contains(
                                    x.CandidateId),

                            CvDownloaded =
                                downloadedCandidates.Contains(
                                    x.CandidateId)
                        })
                    .ToList()
            };
        }

        // ==========================================================
        // TODO (Part 3)
        // ==========================================================
        public async Task<UpdateApplicantStatusResponseDto> MoveToReviewAsync(
         Guid employerId,
         Guid applicationId,
         UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.InReview;
            application.StatusUpdatedAt = DateTime.UtcNow;

            // Always overwrite old note with new one (or clear if none provided)
            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant moved to review.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> ShortlistApplicantAsync(
            Guid employerId,
            Guid applicationId,
            UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.IsShortlisted = true;
            application.ShortlistedAt = DateTime.UtcNow;
            application.ApplicationStatus = ApplicationStatus.Shortlisted;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant shortlisted successfully.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> ScheduleInterviewAsync(
            Guid employerId,
            Guid applicationId,
            ScheduleInterviewRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.InterviewScheduledAt = request.InterviewDate;
            application.ApplicationStatus = ApplicationStatus.Interview;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Interview scheduled successfully.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> MoveToTableInterviewAsync(
            Guid employerId,
            Guid applicationId,
            UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.TableInterview;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant moved to table interview.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> MoveToCvSelectionAsync(
            Guid employerId,
            Guid applicationId,
            UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.CvSelection;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant moved to CV selection.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> MoveToLocationInterviewAsync(
            Guid employerId,
            Guid applicationId,
            UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.LocationInterview;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant moved to location interview.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> RejectApplicantAsync(
            Guid employerId,
            Guid applicationId,
            RejectApplicantRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.Rejected;
            application.RejectedAt = DateTime.UtcNow;
            application.StatusUpdatedAt = DateTime.UtcNow;

            // Overwrite old note — use Reason if provided, else Note, else clear
            application.EmployerInternalNote = !string.IsNullOrWhiteSpace(request.Reason)
                ? request.Reason
                : request.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant rejected successfully.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        public async Task<UpdateApplicantStatusResponseDto> HireApplicantAsync(
            Guid employerId,
            Guid applicationId,
            UpdateApplicantNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            application.ApplicationStatus = ApplicationStatus.Hired;
            application.StatusUpdatedAt = DateTime.UtcNow;

            application.EmployerInternalNote = request?.Note;

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Applicant hired successfully.",
                ApplicationId = application.ApplicationId,
                ApplicationStatus = application.ApplicationStatus.ToString()
            };
        }

        // ==========================================================
        // TODO (Part 4)
        // ==========================================================
        public async Task<UpdateApplicantStatusResponseDto> AddRecruiterNoteAsync(
     Guid employerId,
     Guid applicationId,
     AddRecruiterNoteRequestDto request)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId);

            if (application == null)
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            var note = new RecruiterNote
            {
                RecruiterNoteId = Guid.NewGuid(),

                ApplicationId = applicationId,

                EmployerId = employerId,

                NoteText = request.NoteText,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow,

                IsAcknowledged = false
            };

            _context.RecruiterNotes.Add(note);

            await _context.SaveChangesAsync();

            return new UpdateApplicantStatusResponseDto
            {
                Success = true,
                Message = "Note added successfully.",
                ApplicationId = applicationId,
                ApplicationStatus =
                    application.ApplicationStatus.ToString()
            };
        }

        public async Task<RecruiterNotesResponseDto> GetRecruiterNotesAsync(
      Guid employerId,
      Guid applicationId)
        {
            var notes = await _context.RecruiterNotes
                .AsNoTracking()
                .Where(x =>
                    x.ApplicationId == applicationId &&
                    x.EmployerId == employerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new RecruiterNotesResponseDto
            {
                Notes = notes
                    .Select(x =>
                        new RecruiterNoteItemDto
                        {
                            RecruiterNoteId =
                                x.RecruiterNoteId,

                            NoteText =
                                x.NoteText,

                            CreatedAt =
                                x.CreatedAt
                        })
                    .ToList()
            };
        }
    }
}