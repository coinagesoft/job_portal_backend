using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public;

[ApiController]
[Route("api/candidate/public/companies")]
public class PublicCompanyController : ControllerBase
{
    private readonly IPublicCompanyService _companyService;

    public PublicCompanyController(
        IPublicCompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Public company listing (Before Login)
    /// </summary>
    [HttpGet("GetCompanies")]
    public async Task<IActionResult> GetCompanies()
    {
        var result =
            await _companyService.GetCompaniesAsync();

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Public company profile
    /// </summary>
    [HttpGet("GetCompany/details/{employerId:guid}")]
    public async Task<IActionResult> GetCompany(
        Guid employerId)
    {
        var result =
            await _companyService.GetCompanyDetailAsync(
                employerId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}