using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Application.DTOs.Candidate;

/// <summary>
/// One endpoint for every document type. The client identifies the document
/// via <see cref="DocumentType"/> and sends the file in <see cref="Document"/>.
/// </summary>
public class UploadDocumentRequest
{
    /// <summary>e.g. "Aadhaar", "Passport", "EducationCertificate".</summary>
    public string DocumentType { get; set; } = string.Empty;

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