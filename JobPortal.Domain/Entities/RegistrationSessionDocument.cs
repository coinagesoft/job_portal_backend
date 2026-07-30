using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class RegistrationSessionDocument
    {
        [Key]
        public Guid RegistrationDocumentId { get; set; } = Guid.NewGuid();

        // Registration Session
        public Guid SessionId { get; set; }
        public RegistrationSession Session { get; set; } = default!;

        // Admin Document Type (null when recruiter uploads custom document)
        public Guid? DocumentTypeId { get; set; }
        public VerificationDocumentMaster? DocumentType { get; set; }

        // For custom documents
        public string? CustomDocumentName { get; set; }
        public string? Category { get; set; }

        // AI Parsing
        public string? DetectedDocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        public string? ParsedDataJson { get; set; }
        public decimal? AiConfidenceScore { get; set; }

        // Uploaded File
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? PublicId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }
}
