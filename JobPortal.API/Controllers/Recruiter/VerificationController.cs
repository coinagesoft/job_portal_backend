using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/verification")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterVerificationController : ControllerBase
    {
        private readonly IVerificationService _verificationService;

        public RecruiterVerificationController(IVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        /// <summary>
        /// Get verification dashboard
        /// </summary>
        [HttpGet("{employerId:guid}")]
        public async Task<IActionResult> GetVerificationDashboard(Guid employerId)
        {
            var result = await _verificationService
                .GetVerificationDashboardAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Employer profile not found."
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Upload verification document
        /// </summary>
        [HttpPost("{employerId:guid}/document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(
            Guid employerId,
            [FromForm] UploadVerificationDocumentRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Please select a file."
                });
            }

            var result = await _verificationService
                .UploadDocumentAsync(
                    employerId,
                    request);

            if (!result)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Employer profile not found."
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Document uploaded successfully."
            });
        }

        /// <summary>
        /// View uploaded document
        /// </summary>
        [HttpGet("{employerId:guid}/document/{documentType}")]
        public async Task<IActionResult> GetDocument(
            Guid employerId,
            DocumentType documentType)
        {
            var result = await _verificationService
                .GetDocumentAsync(
                    employerId,
                    documentType);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Document not found."
                });
            }

            return Ok(result);
        }
    }
}