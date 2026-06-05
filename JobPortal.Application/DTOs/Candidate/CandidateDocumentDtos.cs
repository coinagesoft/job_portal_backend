// ============================================================
//  JobPortal.Application/DTOs/Candidate/Profile/
//  CandidateDocumentDtos.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Profile;

// ─────────────────────────────────────────────────────────────
// SECTION 2 — DOCUMENTS
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Full list of all uploaded documents for the candidate.
/// GET /api/candidate/profile/documents
/// </summary>
public class CandidateDocumentsResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidateDocumentsData? Data { get; set; }
}

public class CandidateDocumentsData
{
    public ResumeDocumentDto?            Resume               { get; set; }
    public List<EducationCertificateDto> EducationCertificates { get; set; } = new();
    public PassportDocumentDto?          Passport             { get; set; }
    public AadhaarDocumentDto?           Aadhaar              { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 2A — Resume
// POST   /api/candidate/profile/documents/resume          (upload)
// DELETE /api/candidate/profile/documents/resume          (delete)
// ─────────────────────────────────────────────────────────────

public class ResumeDocumentDto
{
    public Guid    CvId          { get; set; }
    public string? CvFileUrl     { get; set; }
    public string? ParsedName    { get; set; }
    public string? ParsedPhone   { get; set; }
    public string? ParsedEmail   { get; set; }
    public string? ParsedTrade   { get; set; }
    public int?    ParsedExperienceYrs { get; set; }
    public string? ParsedSkills  { get; set; }     // JSON string
    public decimal? AiConfidenceScore { get; set; }
    public DateTime? UploadedAt  { get; set; }
    public string  VerificationStatus { get; set; } = "Pending"; // Pending|Verified|Rejected
}

public class UploadResumeResponseDto
{
    public bool   Success      { get; set; }
    public string Message      { get; set; } = string.Empty;
    public Guid?  CvId         { get; set; }
    public string? CvFileUrl   { get; set; }
    public byte   ProfileCompletionPct { get; set; }
}

public class DeleteResumeResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// 2B — Education Certificates
// POST   /api/candidate/profile/documents/education-certificate       (upload / link to education entry)
// DELETE /api/candidate/profile/documents/education-certificate/{id}  (delete one)
// GET    /api/candidate/profile/documents/education-certificate       (list all)
// ─────────────────────────────────────────────────────────────

public class EducationCertificateDto
{
    public Guid   EducationId       { get; set; }
    public string EducationLevel    { get; set; } = string.Empty;  // 10th|12th|ITI|Diploma|Graduate|Other
    public string? InstituteName    { get; set; }
    public string? MarksPercentage  { get; set; }
    public short?  PassoutYear      { get; set; }
    public string? CertificateUrl   { get; set; }
    public string  VerificationStatus { get; set; } = "Pending";  // Pending|Verified|Rejected
    public DateTime CreatedAt       { get; set; }
}

public class UploadEducationCertificateRequestDto
{
    [Required]
    public string EducationLevel { get; set; } = string.Empty;   // 10th|12th|ITI|Diploma|Graduate|Other

    [MaxLength(200)]
    public string? InstituteName { get; set; }

    [MaxLength(10)]
    public string? MarksPercentage { get; set; }

    [Range(1950, 2100)]
    public short? PassoutYear { get; set; }

    // IFormFile passed separately in [FromForm]
}

public class UploadEducationCertificateResponseDto
{
    public bool   Success        { get; set; }
    public string Message        { get; set; } = string.Empty;
    public Guid?  EducationId    { get; set; }
    public string? CertificateUrl { get; set; }
    public byte   ProfileCompletionPct { get; set; }
}

public class DeleteEducationCertificateResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// 2C — Passport
// POST   /api/candidate/profile/documents/passport   (upload)
// DELETE /api/candidate/profile/documents/passport   (delete)
// ─────────────────────────────────────────────────────────────

public class PassportDocumentDto
{
    public Guid    VerificationId   { get; set; }
    public string? FrontImageUrl    { get; set; }
    public string? BackImageUrl     { get; set; }
    public string? AiExtractedName  { get; set; }
    public DateOnly? AiExtractedDob { get; set; }
    public string  AdminDecision    { get; set; } = "Pending";  // Pending|Approved|Rejected
    public string? RejectionReason  { get; set; }
    public DateTime UploadedAt      { get; set; }
}

public class UploadPassportRequestDto
{
    // IFormFile fields (FrontImage required, BackImage optional) passed via [FromForm]
    [Required]
    public bool ConsentGiven { get; set; }  // PDPA / GDPR consent for ID data
}

public class UploadPassportResponseDto
{
    public bool   Success          { get; set; }
    public string Message          { get; set; } = string.Empty;
    public Guid?  VerificationId   { get; set; }
    public string? FrontImageUrl   { get; set; }
    public string? BackImageUrl    { get; set; }
    public string AdminDecision    { get; set; } = "Pending";
    public byte   ProfileCompletionPct { get; set; }
}

public class DeletePassportResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// 2D — Aadhaar (KYC)
// POST   /api/candidate/profile/documents/aadhaar   (upload)
// DELETE /api/candidate/profile/documents/aadhaar   (delete)
// ─────────────────────────────────────────────────────────────

public class AadhaarDocumentDto
{
    public Guid    VerificationId    { get; set; }
    public string? FrontImageUrl     { get; set; }
    public string? BackImageUrl      { get; set; }
    public string? AiExtractedName   { get; set; }
    public DateOnly? AiExtractedDob  { get; set; }
    public string? AiExtractedAddress { get; set; }
    public decimal? OcrConfidence    { get; set; }
    public string  AdminDecision     { get; set; } = "Pending";  // Pending|Approved|Rejected
    public string? RejectionReason   { get; set; }
    public DateTime UploadedAt       { get; set; }
}

public class UploadAadhaarRequestDto
{
    // IFormFile fields (FrontImage required, BackImage optional) passed via [FromForm]
    [Required]
    public bool ConsentGiven { get; set; }  // Required: explicit consent to process Aadhaar
}

public class UploadAadhaarResponseDto
{
    public bool   Success          { get; set; }
    public string Message          { get; set; } = string.Empty;
    public Guid?  VerificationId   { get; set; }
    public string? FrontImageUrl   { get; set; }
    public string? BackImageUrl    { get; set; }
    public string AdminDecision    { get; set; } = "Pending";
    public byte   ProfileCompletionPct { get; set; }
}

public class DeleteAadhaarResponseDto
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
