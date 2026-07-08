using JobPortal.Application.DTOs.Recruiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterScreeningQuestionService
    {
        Task<ScreeningQuestionsResponseDto> GetScreeningQuestionsAsync(
            Guid jobId,
            Guid employerId);

        Task<BaseResponseDto> SaveScreeningQuestionsAsync(
            Guid jobId,
            SaveScreeningQuestionsRequestDto request,
            Guid employerId);

        Task<BaseResponseDto> DeleteScreeningQuestionsAsync(
            Guid jobId,
            Guid employerId);

        Task<ApplicationScreeningResponseDto> GetApplicationScreeningAsync(
            Guid applicationId,
            Guid employerId);

        Task<JobScreeningResponseDto> GetJobScreeningAsync(
            Guid jobId,
            Guid employerId);

        Task<RecruiterJobListResponseDto> GetRecruiterJobsAsync(Guid employerId);
  

    }
}
