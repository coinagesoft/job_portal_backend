using JobPortal.Services.AI;
using JobPortal.Services.IImplement.AI;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers;

[ApiController]
[Route("api/test-ai")]
public class TestAiController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Test(
        [FromServices] IEmbeddingService embeddingService)
    {
        var vector =
            await embeddingService.GenerateEmbeddingAsync(
                "Electrician with 5 years experience");

        return Ok(new
        {
            Success = true,
            VectorLength = vector.Length
        });
    }

    [HttpPost("candidate/{candidateId}")]
    public async Task<IActionResult> GenerateCandidate(
        Guid candidateId,
        [FromServices] IEmbeddingStorageService service)
    {
        await service.GenerateCandidateEmbeddingAsync(
            candidateId);

        return Ok("Candidate embedding generated");
    }
    [HttpPost("job/{jobId}")]
    public async Task<IActionResult> GenerateJob(
    Guid jobId,
    [FromServices] IEmbeddingStorageService service)
    {
        await service.GenerateJobEmbeddingAsync(jobId);

        return Ok("Job embedding generated");
    }
    [HttpGet("score")]
    public async Task<IActionResult> Score(
      Guid candidateId,
      Guid jobId,
      [FromServices] IJobMatchingService service)
    {
        var result =
            await service.CalculateMatchAsync(
                candidateId,
                jobId);

        return Ok(result);
    }
}