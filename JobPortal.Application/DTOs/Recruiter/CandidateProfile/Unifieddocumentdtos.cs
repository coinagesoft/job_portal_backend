using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Application.DTOs.Candidate;

/// <summary>
/// One endpoint for every document type. The document type is detected
/// automatically by the OCR parser — the client only sends the file.
/// </summary>
public class UploadDocumentRequest
{
    public IFormFile Document { get; set; } = default!;
}

public class UploadDocumentResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? DocumentId { get; set; }

    public string? DocumentType { get; set; }

    public string? FileUrl { get; set; }

    /// <summary>Name detected on the document.</summary>
    public string? ParsedName { get; set; }

    /// <summary>True only when the parsed name matched the candidate's profile name.</summary>
    public bool NameMatched { get; set; }

    /// <summary>Full parsed field set from the OCR parser.</summary>
    public JsonElement? ParsedData { get; set; }
}
/// <summary>A stored, OCR-verified candidate document (row in candidate_documents).</summary>
public class CandidateUploadedDocumentDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? ParsedName { get; set; }
    public string VerificationStatus { get; set; } = "Verified";
    public DateTime UploadedAt { get; set; }
}