using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    // ── Identity, resolved from the signed JWT ─────────────────
    // Previously these came from client-supplied headers (EmployerId,
    // UserId, IsSubUser), which meant anyone could hand-craft a request
    // with different values. The JWT now carries all three (see
    // RecruiterAuthService / JwtService), so every action below trusts
    // only what the token itself says.

    private Guid GetEmployerId()
    {
        var employerId = User.FindFirst("EmployerId")?.Value;

        if (string.IsNullOrWhiteSpace(employerId))
            throw new UnauthorizedAccessException(
                "Employer ID not found in token.");

        return Guid.Parse(employerId);
    }

    private Guid GetActionUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private bool GetIsSubUser() =>
        User.FindFirst("IsSubUser")?.Value == "true";

    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var result =
            await _service.GetEmployerWalletAsync(
                GetEmployerId());

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("allocate-credits")]
    public async Task<IActionResult> AllocateCredits(
        [FromBody] AllocateCreditsRequestDto request)
    {
        // Only the account owner can allocate credits — matches the
        // Sub-Users page's own "Only the account owner can buy credits
        // or invite users" messaging.
        if (GetIsSubUser())
        {
            return BadRequest(new
            {
                success = false,
                message = "Only the account owner can allocate credits."
            });
        }

        var result =
            await _service.AllocateCreditsAsync(
                GetEmployerId(),
                request);

        return Ok(result);
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/recruiter/my-credit-balance
    //
    // Self-service: returns the CALLER's own allocated/used/remaining
    // credits. Null for the account owner (they draw from the shared
    // wallet directly, not a personal allocation) — the frontend shows
    // the regular wallet cards in that case instead.
    // ════════════════════════════════════════════════════════════
    [HttpGet("my-credit-balance")]
    public async Task<IActionResult> GetMyCreditBalance()
    {
        if (!GetIsSubUser())
        {
            return Ok(null);
        }

        var result = await _service.GetSubUserCreditBalanceAsync(GetActionUserId());

        return Ok(result);
    }

    [HttpPost("candidate/unlock")]
    public async Task<IActionResult> UnlockCandidate(
        [FromBody] UnlockCandidateRequestDto request)
    {
        var result =
            await _service.UnlockCandidateAsync(
                GetEmployerId(),
                GetActionUserId(),
                GetIsSubUser(),
                request);

        return Ok(result);
    }

    [HttpGet("candidate/{candidateId}")]
    public async Task<IActionResult> GetCandidate(
        Guid candidateId)
    {
        var result =
            await _service.GetCandidateProfileAsync(
                GetEmployerId(),
                candidateId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("candidate/download-cv")]
    public async Task<IActionResult> DownloadCv(
        [FromBody] DownloadCvRequestDto request)
    {
        var result =
            await _service.DownloadCvAsync(
                GetEmployerId(),
                GetActionUserId(),
                GetIsSubUser(),
                request);

        return Ok(result);
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/recruiter/candidate/{candidateId}/cv/download
    //
    // Streams the candidate's CV as a watermarked PDF (company name +
    // download date). The watermark is applied in memory and the bytes
    // are discarded after the response — nothing is stored. Only works
    // when the profile is unlocked for this employer.
    // ════════════════════════════════════════════════════════════
    [HttpGet("candidate/{candidateId:guid}/cv/download")]
    public async Task<IActionResult> DownloadWatermarkedCv(
        Guid candidateId)
    {
        var result =
            await _service.DownloadWatermarkedCvAsync(
                GetEmployerId(),
                candidateId);

        if (!result.Success || result.FileBytes == null)
            return BadRequest(new { success = false, message = result.Message });

        return File(result.FileBytes, "application/pdf", result.FileName);
    }


    [HttpGet("credit-usage-history")]
    public async Task<IActionResult> GetCreditUsageHistory()
    {
        var result =
            await _service.GetCreditUsageHistoryAsync(
                GetEmployerId());

        return Ok(result);
    }

    [HttpGet("purchase-history")]
    public async Task<IActionResult> GetPurchaseHistory()
    {
        var result =
            await _service.GetPurchaseHistoryAsync(
                GetEmployerId());

        return Ok(result);
    }

    [HttpGet("allocation-history")]
    public async Task<IActionResult> GetAllocationHistory()
    {
        var result = await _service.GetAllocationHistoryAsync(
            GetEmployerId(), GetActionUserId(), GetIsSubUser());

        return Ok(result);
    }

    [HttpGet("cv-download-history")]
    public async Task<IActionResult> GetCvDownloadHistory()
    {
        var result = await _service.GetCvDownloadHistoryAsync(GetEmployerId());

        return Ok(result);
    }

    [HttpGet("unlocked-candidates")]
    public async Task<IActionResult> GetUnlockedCandidates()
    {
        var result = await _service.GetUnlockedCandidatesAsync(
            GetEmployerId(), GetActionUserId(), GetIsSubUser());

        return Ok(result);
    }

    [HttpGet("transaction-history")]
    public async Task<IActionResult> GetTransactionHistory()
    {
        var result = await _service.GetEmployerTransactionHistoryAsync(
            GetEmployerId(), GetActionUserId(), GetIsSubUser());

        return Ok(result);
    }


    [HttpGet("credit-wallet-dashboard")]
    public async Task<IActionResult> GetCreditWalletDashboard()
    {
        var result = await _service.GetCreditWalletDashboardAsync(GetEmployerId());

        return Ok(result);
    }
}