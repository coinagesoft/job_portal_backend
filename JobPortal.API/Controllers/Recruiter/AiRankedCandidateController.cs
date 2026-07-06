using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Services.IImplement.AI;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

/// <summary>
/// Endpoints for AI-powered candidate ranking and profile scoring.
/// Implements ChatGPT recommendations #3 (score breakdown) and #4 (ranked endpoint).
/// </summary>
[ApiController]
[Route("api/recruiter/ai/candidates")]
public class AiRankedCandidatesController : ControllerBase
{
    private readonly IRankedCandidateService _service;

    public AiRankedCandidatesController(IRankedCandidateService service)
        => _service = service;

    // =====================================================
    // GET /api/recruiter/ai/candidates/ranked?jobId=...&minScore=60&limit=20
    //
    // Returns all candidates ranked by AI match score for a job.
    // Use this on the recruiter CV Search / Applicants page.
    // =====================================================

    /// <summary>
    /// Get candidates ranked by AI match score for a specific job.
    /// Each candidate includes a full score breakdown:
    ///   - OverallScore (0–100)
    ///   - AiSimilarityScore (semantic embedding, 35% weight)
    ///   - SkillScore (30%)
    ///   - TradeScore (20%)
    ///   - ExperienceScore (10%)
    ///   - LocationScore (5%)
    ///   - ScoreLabel (Excellent / Good / Fair / Low)
    /// </summary>
    [HttpGet("ranked")]
    public async Task<IActionResult> GetRankedCandidates(
        [FromQuery] Guid jobId,
        [FromQuery] int minScore = 0,
        [FromQuery] int limit = 20)
    {
        if (jobId == Guid.Empty)
            return BadRequest(new { Message = "jobId is required." });

        var request = new RankedCandidateRequestDto
        {
            JobId = jobId,
            MinScore = Math.Clamp(minScore, 0, 100),
            Limit = Math.Clamp(limit, 1, 100)
        };

        var result = await _service.GetRankedCandidatesAsync(request);
        return Ok(result);
    }

    // =====================================================
    // GET /api/recruiter/ai/candidates/{candidateId}/score?jobId=...
    //
    // Returns the AI score breakdown for ONE candidate vs ONE job.
    // Use this on the candidate profile page in the recruiter portal.
    // =====================================================

    /// <summary>
    /// Get the AI match score breakdown for a single candidate against a job.
    /// Also returns matched and missing skills — great for the profile-view sidebar.
    /// </summary>
    [HttpGet("{candidateId}/score")]
    public async Task<IActionResult> GetCandidateProfileScore(
        Guid candidateId,
        [FromQuery] Guid jobId)
    {
        if (candidateId == Guid.Empty || jobId == Guid.Empty)
            return BadRequest(new { Message = "Both candidateId and jobId are required." });

        var result = await _service.GetCandidateProfileScoreAsync(
            candidateId, jobId);

        if (result == null)
            return NotFound(new { Message = "Candidate or job not found." });

        return Ok(result);
    }
}
