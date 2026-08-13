using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class EmployerVerificationDocument
    {
        [Key]
        public Guid DocumentId { get; set; }

        public Guid EmployerId { get; set; }
        public Guid? RequestId { get; set; }

        public string? CustomDocumentName { get; set; }

        public string? Category { get; set; }
        public string? DocumentNumber { get; set; }

        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public string FileName { get; set; }

        public string FileUrl { get; set; }

        public string PublicId { get; set; }

        public VerificationDocumentStatus Status { get; set; } = VerificationDocumentStatus.Pending;

        public Guid? VerifiedBy { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? Remarks { get; set; }

        public bool IsDeleted { get; set; }

        // NEW
        public string? DetectedDocumentType { get; set; }

        // NEW
        public decimal? AiConfidenceScore { get; set; }

        // NEW
        public string? ParsedDataJson { get; set; }

        // Navigation
        public EmployerProfile Employer { get; set; } = default!;

        public ICollection<EmployerBadge> Badges { get; set; }
            = new List<EmployerBadge>();

        public Guid? DocumentTypeId { get; set; }

        public VerificationDocumentMaster? DocumentType { get; set; } = default!;

        public EmployerDocumentRequest? Request { get; set; }

    }
}
