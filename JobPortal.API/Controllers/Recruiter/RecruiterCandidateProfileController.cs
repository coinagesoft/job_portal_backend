using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/candidates")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterCandidateProfileController : ControllerBase
    {
        private readonly IRecruiterCandidateProfileService
            _candidateProfileService;

        public RecruiterCandidateProfileController(
            IRecruiterCandidateProfileService candidateProfileService)
        {
            _candidateProfileService =
                candidateProfileService;
        }

        /// <summary>
        /// Get complete recruiter candidate profile
        /// </summary>
        [HttpGet("{candidateId}/full-profile")]
        public async Task<IActionResult> GetFullProfile(
            Guid candidateId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _candidateProfileService
                .GetFullProfileAsync(
                    employerId,
                    candidateId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Candidate not found."
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get candidate unlock status
        /// </summary>
        [HttpGet("{candidateId}/unlock-status")]
        public async Task<IActionResult> GetUnlockStatus(
            Guid candidateId,
            [FromQuery] Guid employerId)
        {
            var result =
                await _candidateProfileService
                .GetUnlockStatusAsync(
                    employerId,
                    candidateId);

            return Ok(result);
        }
    }
}