using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentsResponseDto
    {
        public Guid EmployerId { get; set; }

        public string CompanyName { get; set; } = default!;

        public string? CompanyLogoUrl { get; set; }

        public string? Gstin { get; set; }

        public DateTime RegisteredAt { get; set; }

        public string? City { get; set; }

        public string Country { get; set; } = "India";

        public RecruiterDocumentVerificationSummaryDto Verification { get; set; }
            = new();

        // ONLY documents that have actually been uploaded
        public List<AdminRecruiterDocumentDto> Documents { get; set; }
            = new();
    }


    public class RecruiterDocumentVerificationSummaryDto
    {
        // Total active mandatory master document types
        public int Total { get; set; }

        // Mandatory documents with Approved status
        public int Verified { get; set; }

        // Mandatory documents uploaded but not yet approved/rejected
        public int Pending { get; set; }

        // Mandatory documents with no uploaded document
        public int NotUploaded { get; set; }

        // Mandatory documents with latest upload Rejected
        public int Rejected { get; set; }


        // Overall verification status
        public string Status { get; set; } = "Pending";
    }


    public class AdminRecruiterDocumentDto
    {
        public Guid DocumentId { get; set; }

        // Null for custom documents
        public Guid? DocumentTypeId { get; set; }

        // Present when document was uploaded against an admin request
        public Guid? RequestId { get; set; }

        public string DocumentName { get; set; } = default!;

        // Mandatory / Optional / Additional / RequestedAdditional
        public string? Category { get; set; }
        public string DocumentCategory { get; set; } = "Additional";
        public string? DocumentNumber { get; set; }

        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public bool IsExpired { get; set; }

        public string FileName { get; set; } = default!;

        public string FileUrl { get; set; } = default!;

        public string PublicId { get; set; } = default!;

        // Pending / Approved / Rejected / Expired / Resubmission
        public string Status { get; set; } = default!;

        public Guid? VerifiedBy { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? Remarks { get; set; }

        public string? DetectedDocumentType { get; set; }
        public decimal? AiExtractionPercentage { get; set; }

        public bool RequiresVerification { get; set; }

        public bool IsMandatory { get; set; }
    }
}