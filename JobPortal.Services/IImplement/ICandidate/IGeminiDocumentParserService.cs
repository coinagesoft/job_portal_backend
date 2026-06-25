using JobPortal.Application.DTOs.Candidate;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.ICandidate;

public interface IGeminiDocumentParserService
{
    Task<GeminiDocumentParseResponse> ParseDocumentAsync(
        IFormFile document);
}