using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Authorize(Roles = "Recruiter")]
    [Route("api/recruiter/screening-questions")]
    public class RecruiterScreeningQuestionController : ControllerBase
    {
        private readonly IRecruiterScreeningQuestionService _screeningQuestionService;

        public RecruiterScreeningQuestionController(
            IRecruiterScreeningQuestionService screeningQuestionService)
        {
            _screeningQuestionService = screeningQuestionService;
        }

        private Guid GetEmployerId()
        {
            var employerId = User.FindFirst("EmployerId")?.Value;

            if (string.IsNullOrWhiteSpace(employerId))
                throw new UnauthorizedAccessException(
                    "Employer ID not found in token.");

            return Guid.Parse(employerId);
        }

        /// <summary>
        /// Save or Update Screening Questions
        /// </summary>
        [HttpPut("{jobId:guid}")]
        public async Task<IActionResult> SaveScreeningQuestions(
            Guid jobId,
            [FromBody] SaveScreeningQuestionsRequestDto request)
        {
            var result = await _screeningQuestionService.SaveScreeningQuestionsAsync(
                jobId,
                request,
                GetEmployerId());

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get Screening Questions
        /// </summary>
        [HttpGet("{jobId:guid}")]
        public async Task<IActionResult> GetScreeningQuestions(Guid jobId)
        {
            var result = await _screeningQuestionService.GetScreeningQuestionsAsync(
                jobId,
                GetEmployerId());

            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Delete Screening Questions
        /// </summary>
        [HttpDelete("{jobId:guid}")]
        public async Task<IActionResult> DeleteScreeningQuestions(Guid jobId)
        {
            var result = await _screeningQuestionService.DeleteScreeningQuestionsAsync(
                jobId,
                GetEmployerId());

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get Screening Questions & Answers for One Application
        /// </summary>
        [HttpGet("applications/{applicationId:guid}")]
        public async Task<IActionResult> GetApplicationScreening(Guid applicationId)
        {
            var result = await _screeningQuestionService.GetApplicationScreeningAsync(
                applicationId,
                GetEmployerId());

            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get Screening Details for All Applications of a Job
        /// </summary>
        [HttpGet("jobs/{jobId:guid}/applications")]
        public async Task<IActionResult> GetJobScreening(Guid jobId)
        {
            var result = await _screeningQuestionService.GetJobScreeningAsync(
                jobId,
                GetEmployerId());

            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetRecruiterJobs()
        {
            var result = await _screeningQuestionService
                .GetRecruiterJobsAsync(GetEmployerId());

            return result.Success ? Ok(result) : BadRequest(result);
        }

     
    }
}