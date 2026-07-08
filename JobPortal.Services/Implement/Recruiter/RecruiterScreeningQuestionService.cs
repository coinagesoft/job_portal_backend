using Google;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterScreeningQuestionService : IRecruiterScreeningQuestionService
    {
        private readonly AppDbContext _context;

        public RecruiterScreeningQuestionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RecruiterJobListResponseDto> GetRecruiterJobsAsync(Guid employerId)
        {
            try
            {
                var jobs = await _context.JobPostings
                    .AsNoTracking()
                    .Where(j =>
                        j.EmployerId == employerId &&
                        !j.IsDeleted)
                    .OrderByDescending(j => j.CreatedAt)
                    .Select(j => new RecruiterJobDto
                    {
                        JobId = j.JobId,
                        JobTitle = j.JobTitle,
                        JobStatus = j.JobStatus.ToString(),
                        TotalApplications = j.Applications.Count,
                        CreatedAt = j.CreatedAt,

                        ApplicationIds = j.Applications
                            .Select(a => a.ApplicationId)
                            .ToList()
                    })
                    .ToListAsync();

                return new RecruiterJobListResponseDto
                {
                    Success = true,
                    Message = jobs.Any()
                        ? "Recruiter jobs retrieved successfully."
                        : "No jobs found.",
                    Jobs = jobs
                };
            }
            catch (Exception ex)
            {
                return new RecruiterJobListResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while retrieving recruiter jobs. {ex.Message}"
                };
            }
        }
        public async Task<ScreeningQuestionsResponseDto> GetScreeningQuestionsAsync(
    Guid jobId,
    Guid employerId)
        {
            try
            {
                // Debug 1: Check if Job exists
                var jobById = await _context.JobPostings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobId == jobId);

                if (jobById == null)
                {
                    return new ScreeningQuestionsResponseDto
                    {
                        Success = false,
                        Message = $"DEBUG: Job not found.\nJobId Passed = {jobId}"
                    };
                }

                // Debug 2: Check EmployerId
                if (jobById.EmployerId != employerId)
                {
                    return new ScreeningQuestionsResponseDto
                    {
                        Success = false,
                        Message = $"DEBUG: EmployerId mismatch.\n" +
                                  $"Passed EmployerId : {employerId}\n" +
                                  $"DB EmployerId     : {jobById.EmployerId}"
                    };
                }

                // Debug 3: Check IsDeleted
                if (jobById.IsDeleted)
                {
                    return new ScreeningQuestionsResponseDto
                    {
                        Success = false,
                        Message = $"DEBUG: Job is marked as deleted."
                    };
                }

                // Original Query
                var job = await _context.JobPostings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j =>
                        j.JobId == jobId &&
                        j.EmployerId == employerId &&
                        !j.IsDeleted);

                if (job == null)
                {
                    return new ScreeningQuestionsResponseDto
                    {
                        Success = false,
                        Message = "DEBUG: Final query returned null."
                    };
                }

                return new ScreeningQuestionsResponseDto
                {
                    Success = true,
                    Message = "Screening questions retrieved successfully.",
                    JobId = job.JobId,
                    Questions = job.ScreeningQuestions ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                return new ScreeningQuestionsResponseDto
                {
                    Success = false,
                    Message = $"Exception: {ex}",
                    Questions = new List<string>()
                };
            }
        }

        public async Task<BaseResponseDto> SaveScreeningQuestionsAsync(
        Guid jobId,
        SaveScreeningQuestionsRequestDto request,
        Guid employerId)
        {
            try
            {
                var job = await _context.JobPostings
                    .FirstOrDefaultAsync(j =>
                        j.JobId == jobId &&
                        j.EmployerId == employerId &&
                        !j.IsDeleted);

                if (job == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };
                }

                job.ScreeningQuestions = request.Questions?.Any() == true
                    ? request.Questions
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .Select(q => q.Trim())
                        .ToList()
                    : new List<string>();

                job.UpdatedAt = DateTime.UtcNow;

                _context.JobPostings.Update(job);
                await _context.SaveChangesAsync();

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Screening questions saved successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while saving screening questions. {ex.Message}"
                };
            }
        }

        public async Task<BaseResponseDto> DeleteScreeningQuestionsAsync(
      Guid jobId,
      Guid employerId)
        {
            try
            {
                var job = await _context.JobPostings
                    .FirstOrDefaultAsync(j =>
                        j.JobId == jobId &&
                        j.EmployerId == employerId &&
                        !j.IsDeleted);

                if (job == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };
                }

                job.ScreeningQuestions = new List<string>();
                job.UpdatedAt = DateTime.UtcNow;

                _context.JobPostings.Update(job);
                await _context.SaveChangesAsync();

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Screening questions deleted successfully."
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while deleting screening questions. {ex.Message}"
                };
            }
        }

        public async Task<ApplicationScreeningResponseDto> GetApplicationScreeningAsync(
          Guid applicationId,
          Guid employerId)
        {
            try
            {
                var application = await _context.JobApplications
                    .Include(a => a.JobPosting)
                    .Include(a => a.CandidateProfile)
                    .FirstOrDefaultAsync(a =>
                        a.ApplicationId == applicationId &&
                        a.EmployerId == employerId);

                if (application == null)
                {
                    return new ApplicationScreeningResponseDto
                    {
                        Success = false,
                        Message = "Application not found."
                    };
                }

                var questions = application.JobPosting.ScreeningQuestions ?? new List<string>();
                var answers = application.ScreeningAnswers ?? new List<string>();

                var screening = new List<ScreeningQuestionAnswerDto>();

                for (int i = 0; i < questions.Count; i++)
                {
                    string? answer = i < answers.Count ? answers[i] : null;

                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        var prefix = questions[i] + ":";

                        if (answer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            answer = answer.Substring(prefix.Length).Trim();
                        }
                    }

                    screening.Add(new ScreeningQuestionAnswerDto
                    {
                        Question = questions[i],
                        Answer = answer
                    });
                }

                return new ApplicationScreeningResponseDto
                {
                    Success = true,
                    Message = "Screening details retrieved successfully.",
                    ApplicationId = application.ApplicationId,
                    JobId = application.JobId,
                    JobTitle = application.JobPosting.JobTitle,
                    CandidateId = application.CandidateId,
                    CandidateName = application.CandidateProfile.FullName,
                    Screening = screening
                };
            }
            catch (Exception ex)
            {
                return new ApplicationScreeningResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while retrieving screening details. {ex.Message}"
                };
            }
        }

        public async Task<JobScreeningResponseDto> GetJobScreeningAsync(
         Guid jobId,
         Guid employerId)
        {
            try
            {
                var job = await _context.JobPostings
                    .Include(j => j.Applications)
                        .ThenInclude(a => a.CandidateProfile)
                    .FirstOrDefaultAsync(j =>
                        j.JobId == jobId &&
                        j.EmployerId == employerId &&
                        !j.IsDeleted);

                if (job == null)
                {
                    return new JobScreeningResponseDto
                    {
                        Success = false,
                        Message = "Job not found."
                    };
                }

                var response = new JobScreeningResponseDto
                {
                    Success = true,
                    Message = "Job screening details retrieved successfully.",
                    JobId = job.JobId,
                    JobTitle = job.JobTitle,
                    TotalApplications = job.Applications.Count
                };

                var questions = job.ScreeningQuestions ?? new List<string>();

                foreach (var application in job.Applications)
                {
                    var answers = application.ScreeningAnswers ?? new List<string>();

                    var screening = new List<ScreeningQuestionAnswerDto>();

                    for (int i = 0; i < questions.Count; i++)
                    {
                        string? answer = i < answers.Count ? answers[i] : null;

                        if (!string.IsNullOrWhiteSpace(answer))
                        {
                            var prefix = questions[i] + ":";

                            if (answer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                answer = answer.Substring(prefix.Length).Trim();
                            }
                        }

                        screening.Add(new ScreeningQuestionAnswerDto
                        {
                            Question = questions[i],
                            Answer = answer
                        });
                    }

                    response.Applications.Add(new JobApplicationScreeningDto
                    {
                        ApplicationId = application.ApplicationId,
                        CandidateId = application.CandidateId,
                        CandidateName = application.CandidateProfile.FullName,
                        ApplicationStatus = application.ApplicationStatus,
                        AppliedAt = application.AppliedAt,
                        Screening = screening
                    });
                }

                return response;
            }
            catch (Exception ex)
            {
                return new JobScreeningResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while retrieving screening details. {ex.Message}"
                };
            }
        }
    }
}