using System;

namespace JobPortal.Domain.Entities;

/// <summary>
/// A single uploaded + OCR-parsed candidate document of any type
/// (Aadhaar, Passport, Education Certificate, …), identified by
/// <see cref="DocumentType"/>. One row per (candidate, document type).
/// </summary>
public class CandidateDocument
{
    public Guid DocumentId { get; set; }

    public Guid CandidateId { get; set; }

    /// <summary>The document type supplied by the client (e.g. "Aadhaar", "Passport").</summary>
    public string DocumentType { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FilePublicId { get; set; } = string.Empty;

    /// <summary>Name extracted from the document by OCR.</summary>
    public string? ParsedName { get; set; }

    /// <summary>Full parsed field set returned by the OCR parser, stored as JSON.</summary>
    public string? ParsedDataJson { get; set; }

    /// <summary>Verified once the parsed name matches the candidate's profile name.</summary>
    public string VerificationStatus { get; set; } = "Verified";

    public DateTime UploadedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CandidateProfile? Candidate { get; set; }
}