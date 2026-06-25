using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JobPortal.Application.DTOs.Candidate;
namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/profile/documents")]
public class CandidateDocumentParserController : ControllerBase
{
    private readonly IGeminiDocumentParserService _geminiService;

    public CandidateDocumentParserController(
        IGeminiDocumentParserService geminiService)
    {
        _geminiService = geminiService;
    }

    /// <summary>
    /// Upload any government document (PDF/Image) and parse it using Gemini.
    /// </summary>
    [HttpPost("parse")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ParseDocument(
     [FromForm] GeminiDocumentRequest request)
    {
        if (request.Document == null || request.Document.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Please upload a document."
            });
        }

        var result = await _geminiService.ParseDocumentAsync(request.Document);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}