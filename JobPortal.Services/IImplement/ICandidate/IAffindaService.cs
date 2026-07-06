using JobPortal.Application.DTOs.AI;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.ICandidate;

public interface IAffindaService
{
    Task<AffindaParseResult> ParseResumeAsync(
        IFormFile file);
}