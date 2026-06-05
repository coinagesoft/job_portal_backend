using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

[ApiController]
[Route("api/employer")]
public class RecruiterCreditController : ControllerBase
{
    private readonly ICreditWalletService _service;

    public RecruiterCreditController(
        ICreditWalletService service)
    {
        _service = service;
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet(
        [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result =
            await _service.GetEmployerWalletAsync(
                employerId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("allocate-credits")]
    public async Task<IActionResult> AllocateCredits(
        [FromHeader(Name = "EmployerId")] Guid employerId,
        [FromBody] AllocateCreditsRequestDto request)
    {
        var result =
            await _service.AllocateCreditsAsync(
                employerId,
                request);

        return Ok(result);
    }

    [HttpPost("candidate/unlock")]
    public async Task<IActionResult> UnlockCandidate(
        [FromHeader(Name = "EmployerId")] Guid employerId,
        [FromHeader(Name = "UserId")] Guid actionUserId,
        [FromHeader(Name = "IsSubUser")] bool isSubUser,
        [FromBody] UnlockCandidateRequestDto request)
    {
        var result =
            await _service.UnlockCandidateAsync(
                employerId,
                actionUserId,
                isSubUser,
                request);

        return Ok(result);
    }

    [HttpGet("candidate/{candidateId}")]
    public async Task<IActionResult> GetCandidate(
        Guid candidateId,
        [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result =
            await _service.GetCandidateProfileAsync(
                employerId,
                candidateId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("candidate/download-cv")]
    public async Task<IActionResult> DownloadCv(
        [FromHeader(Name = "EmployerId")] Guid employerId,
        [FromHeader(Name = "UserId")] Guid actionUserId,
        [FromHeader(Name = "IsSubUser")] bool isSubUser,
        [FromBody] DownloadCvRequestDto request)
    {
        var result =
            await _service.DownloadCvAsync(
                employerId,
                actionUserId,
                isSubUser,
                request);

        return Ok(result);
    }
}