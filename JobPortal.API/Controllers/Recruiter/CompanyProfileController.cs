using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/company-profile")]
    [Authorize(Roles = "Recruiter")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ICompanyProfileService _companyProfileService;

        public CompanyProfileController(
            ICompanyProfileService companyProfileService)
        {
            _companyProfileService = companyProfileService;
        }

        private bool GetIsSubUser() =>
            User.FindFirst("IsSubUser")?.Value == "true";

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
            // Company profile edits are restricted to the account owner —
            // no sub-user, regardless of their individual permission flags,
            // may change these details. Sub-users can still view the
            // profile via GetCompanyProfile above.
            if (GetIsSubUser())
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = "You don't have permission to edit the company profile. Please contact your account owner."
                });
            }

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