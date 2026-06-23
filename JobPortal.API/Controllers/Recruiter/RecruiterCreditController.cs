using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

[ApiController]
[Route("api/recruiter")]
[Authorize(Roles = "Recruiter")]

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


    [HttpGet("credit-usage-history")]
    public async Task<IActionResult> GetCreditUsageHistory(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result =
            await _service.GetCreditUsageHistoryAsync(
                employerId);

        return Ok(result);
    }

    [HttpGet("purchase-history")]
    public async Task<IActionResult> GetPurchaseHistory(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result =
            await _service.GetPurchaseHistoryAsync(
                employerId);

        return Ok(result);
    }

    [HttpGet("allocation-history")]
    public async Task<IActionResult> GetAllocationHistory(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result =
            await _service.GetAllocationHistoryAsync(
                employerId);

        return Ok(result);
    }

    [HttpGet("cv-download-history")]
    public async Task<IActionResult> GetCvDownloadHistory(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result = await _service.GetCvDownloadHistoryAsync(employerId);

        return Ok(result);
    }

    [HttpGet("unlocked-candidates")]
    public async Task<IActionResult> GetUnlockedCandidates(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result = await _service.GetUnlockedCandidatesAsync( employerId);

        return Ok(result);
    }

    [HttpGet("transaction-history")]
    public async Task<IActionResult> GetTransactionHistory(
    [FromHeader(Name = "EmployerId")] Guid employerId)
    {
        var result = await _service.GetEmployerTransactionHistoryAsync(employerId);

        return Ok(result);
    }


    [HttpGet("credit-wallet-dashboard")]
    public async Task<IActionResult>GetCreditWalletDashboard(
        [FromHeader(Name = "EmployerId")]Guid employerId)
    {
        var result = await _service.GetCreditWalletDashboardAsync(employerId);

        return Ok(result);
    }
}