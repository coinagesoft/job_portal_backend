using JobPortal.Application.DTOs.Candidate.Auth;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.Implement.Candidate;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Candidate
{
    [ApiController]
    [Route("api/candidate/registration")]
    public class CandidateRegistrationController : ControllerBase
    {
        private readonly ICandidateRegistrationService _candidateRegistrationService;

        public CandidateRegistrationController(
            ICandidateRegistrationService candidateRegistrationService)
        {
            _candidateRegistrationService = candidateRegistrationService;
        }


        [HttpPost("google")]
        public async Task<IActionResult> GoogleRegister(
            CandidateGoogleRegisterRequestDto request)
        {
            var result = await _candidateRegistrationService.GoogleRegisterAsync(
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return result.Success ? Ok(result) : BadRequest(result);
        }




        [HttpPost("linkedin")]
        public async Task<IActionResult> LinkedInRegister(
            CandidateLinkedInRegisterRequestDto request)
        {
            var result = await _candidateRegistrationService.LinkedInRegisterAsync(
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("google/verify")]
        public async Task<IActionResult> GoogleVerify(GoogleVerifyRequestDto request)
        {
            var result = await _candidateRegistrationService.GoogleVerifyAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("linkedin/verify")]
        public async Task<IActionResult> LinkedInVerify(LinkedInVerifyRequestDto request)
        {
            var result = await _candidateRegistrationService.LinkedInVerifyAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}