using JobPortal.Domain.Enums.common;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public;

// Read-only, unauthenticated plan listings used by the recruiter and
// candidate pricing pages before sign-up/login. Prices reflect
// whatever the admin configured for the requested region.
[ApiController]
[Route("api/plans")]
[AllowAnonymous]
public class PublicMembershipPlanController : ControllerBase
{
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly IRecruiterCreditPlanService _creditPlanService;

    public PublicMembershipPlanController(
        IMembershipPlanService membershipPlanService,
        IRecruiterCreditPlanService creditPlanService)
    {
        _membershipPlanService = membershipPlanService;
        _creditPlanService = creditPlanService;
    }

    // ── GET /api/plans/recruiter?region=us ─────────────────────────
    /// <summary>
    /// Active recruiter (employer) lifetime membership plans for a region.
    /// </summary>
    [HttpGet("recruiter")]
    public async Task<IActionResult> GetRecruiterPlans([FromQuery] string? region = null)
    {
        var plans = await _membershipPlanService.GetActivePlansAsync(PlanType.Recruiter, region);
        return Ok(plans);
    }

    // ── GET /api/plans/candidate?region=us ─────────────────────────
    /// <summary>
    /// Active candidate lifetime membership plans for a region.
    /// </summary>
    [HttpGet("candidate")]
    public async Task<IActionResult> GetCandidatePlans([FromQuery] string? region = null)
    {
        var plans = await _membershipPlanService.GetActivePlansAsync(PlanType.Candidate, region);
        return Ok(plans);
    }

    // ── GET /api/plans/credits?region=us ────────────────────────────
    /// <summary>
    /// Active recruiter credit packs for a region.
    /// </summary>
    [HttpGet("credits")]
    public async Task<IActionResult> GetCreditPlans([FromQuery] string? region = null)
    {
        var plans = await _creditPlanService.GetActivePlansAsync(region);
        return Ok(plans);
    }
}