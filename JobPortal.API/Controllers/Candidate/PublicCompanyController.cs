using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public;

[ApiController]
[Route("api/candidate/public")]
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
    /// 



    [HttpGet("All_Jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllJobs()
    {
        var result =
            await _companyService.GetAllJobsAsync();

        return Ok(result);
    }


    [HttpGet("job_details/{jobId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateJobDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobDetails(Guid jobId)
    {
        var result =
            await _companyService
                .GetJobDetailsAsync(jobId);

        if (result == null)
            return NotFound(new
            {
                Message = "Job not found."
            });

        return Ok(result);
    }


    [HttpGet("filter_by_keywords")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CandidateJobListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs([FromQuery] CandidateJobSearchRequestDto request)
    {
        var result = await _companyService.GetJobsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous]
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
    [AllowAnonymous]
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