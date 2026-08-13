// ============================================================
//  JobPortal.API/Controllers/Public/HomepageController.cs
//  Base route: api/public
// ============================================================

using JobPortal.Application.DTOs.Public;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Public;

[ApiController]
[Route("api/public")]
[Produces("application/json")]
public class HomepageController : ControllerBase
{
    private readonly IHomepageService _homepageService;
    private readonly ILogger<HomepageController> _logger;

    public HomepageController(IHomepageService homepageService, ILogger<HomepageController> logger)
    {
        _homepageService = homepageService;
        _logger = logger;
    }

    /// <summary>
    /// GET api/public/homepage
    /// All homepage sections in one call: BrowseByCategory | LatestJobs | JobsOfTheDay | JobsByRole
    /// Query: ?defaultCountry=India | ?defaultCategory=Construction
    /// Hero search bar → GET api/candidate/jobs?keyword=&tradeCategory=&location=
    /// </summary>
    [HttpGet("homepage")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HomepageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepage([FromQuery] HomepageRequestDto request)
    {
        var result = await _homepageService.GetHomepageDataAsync(request);
        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    /// <summary>
    /// POST api/public/homepage/suggestions
    /// "Don't see your industry/location/role?" — lets a candidate or
    /// recruiter suggest a value for one of the admin-managed homepage
    /// lists. Feeds the admin Suggestions tab. Works whether or not the
    /// caller is logged in.
    /// </summary>
    [HttpPost("homepage/suggestions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SubmitSuggestionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitSuggestion([FromBody] SubmitSuggestionRequestDto request)
    {
        var claim = User.FindFirstValue("candidateId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(claim, out var id) ? id : null;

        var result = await _homepageService.SubmitSuggestionAsync(request, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}