using JobPortal.Domain.Enums.RecruiterEnums;
using System;

namespace JobPortal.Application.DTOs.Recruiter.CompanyDocuments
{
    public class CompanyDocumentResponseDto
    {
        public Guid DocumentId { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public bool IsMasterDocumentType { get; set; }

        public string DocumentName { get; set; } = default!;
        public string Category { get; set; } = default!;

        public string? DocumentNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        public string FileName { get; set; } = default!;
        public string FileUrl { get; set; } = default!;
        public string? DetectedDocumentType { get; set; }

        public decimal? AiConfidenceScore { get; set; }
        public VerificationDocumentStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
