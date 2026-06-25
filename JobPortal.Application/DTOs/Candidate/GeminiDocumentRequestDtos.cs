using Microsoft.AspNetCore.Http;

namespace JobPortal.Application.DTOs.Candidate;

public class GeminiDocumentRequest
{
    public IFormFile Document { get; set; } = default!;
}