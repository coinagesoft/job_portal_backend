using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/company-profile")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ICompanyProfileService _companyProfileService;

        public CompanyProfileController(
            ICompanyProfileService companyProfileService)
        {
            _companyProfileService = companyProfileService;
        }

        [HttpGet("{employerId:guid}")]
        public async Task<IActionResult> GetCompanyProfile(Guid employerId)
        {
            var result = await _companyProfileService
                .GetCompanyProfileAsync(employerId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Company profile not found."
                });
            }

            return Ok(result);
        }

        [HttpPatch("{employerId:guid}")]
        public async Task<IActionResult> UpdateCompanyProfile(
            Guid employerId,
            [FromForm] UpdateCompanyProfileDto request)
        {
            var result = await _companyProfileService
                .UpdateCompanyProfileAsync(employerId, request);

            if (!result)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Company profile not found."
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Company profile updated successfully."
            });
        }
    }
}