using System;

namespace JobPortal.Application.DTOs.Recruiter.CompanyDocuments
{
    public class CompanyDocumentResponseDto
    {
        public Guid DocumentId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        public bool IsMandatory { get; set; }

        public string DocumentName { get; set; } = default!;

        public string Category { get; set; } = default!;

        public string? DocumentNumber { get; set; }

        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public string FileName { get; set; } = default!;

        public string FileUrl { get; set; } = default!;

        public string? PublicId { get; set; }

        public string? DetectedDocumentType { get; set; }

        public decimal? AiConfidenceScore { get; set; }

        public string Status { get; set; } = default!;

        public Guid? VerifiedBy { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? Remarks { get; set; }
    }
}