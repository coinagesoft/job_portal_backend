using JobPortal.Services.IImplement.AI;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin;

[ApiController]
[Route("api/admin/match-debug")]
public class MatchDebugController : ControllerBase
{
    private readonly IJobMatchingService _jobMatching;

    public MatchDebugController(IJobMatchingService jobMatching)
    {
        _jobMatching = jobMatching;
    }

    [HttpGet]
    public async Task<IActionResult> Debug(
        [FromQuery] Guid candidateId,
        [FromQuery] Guid jobId)
    {
        var result = await _jobMatching.CalculateMatchAsync(candidateId, jobId);
        return Ok(result);
    }
}