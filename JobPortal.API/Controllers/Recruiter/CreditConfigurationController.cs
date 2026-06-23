using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

[ApiController]
[Route("api/admin/credit-configuration")]
[Authorize]
public class CreditConfigurationController
    : ControllerBase
{
    private readonly ICreditConfigurationService _service;

    public CreditConfigurationController(
        ICreditConfigurationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult>
        GetConfiguration()
    {
        var result =
            await _service.GetConfigurationAsync();

        if (result == null)
        {
            return NotFound(new
            {
                Success = false,
                Message =
                    "Credit configuration not found."
            });
        }

        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult>
        UpdateConfiguration(
            [FromBody]
            UpdateCreditConfigurationRequestDto request,

            [FromHeader(Name = "AdminId")]
            Guid adminId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result =
            await _service.UpdateConfigurationAsync(
                request,
                adminId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
