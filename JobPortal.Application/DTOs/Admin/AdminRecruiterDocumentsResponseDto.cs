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

        public List<AdminRecruiterDocumentDto> Documents { get; set; }
            = new();
    }


    public class RecruiterDocumentVerificationSummaryDto
    {
        // ==================================================
        // MANDATORY DOCUMENT VERIFICATION SUMMARY
        // ==================================================

        // Total mandatory document types
        public int Total { get; set; }

        // Mandatory documents approved
        public int Verified { get; set; }

        // Mandatory documents uploaded but
        // not yet approved/rejected
        public int Pending { get; set; }

        // Mandatory documents not uploaded
        public int NotUploaded { get; set; }

        // Mandatory documents rejected
        public int Rejected { get; set; }

        public decimal VerificationProgress { get; set; }

        // Overall recruiter verification status
        //
        // Pending / Verified / Rejected
        //
        // No AI percentage here.
        public string Status { get; set; } = "Pending";
    }


    public class AdminRecruiterDocumentDto
    {
        // ==================================================
        // IDENTIFICATION
        // ==================================================

        public Guid DocumentId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        // Important for admin-requested documents.
        //
        // Mandatory/Optional/Additional:
        // null
        //
        // RequestedAdditional:
        // actual RequestId
        public Guid? RequestId { get; set; }


        // ==================================================
        // DOCUMENT NAME
        // ==================================================

        public string DocumentName { get; set; } = default!;


        // ==================================================
        // BUSINESS CATEGORY
        // ==================================================
        //
        // Examples:
        //
        // Tax
        // Licence
        // Registration
        // Other
        //
        public string? Category { get; set; }


        // ==================================================
        // DOCUMENT CATEGORY
        // ==================================================
        //
        // Mandatory
        // Optional
        // Additional
        // RequestedAdditional
        //
        public string DocumentCategory { get; set; }
            = "Additional";


        // ==================================================
        // DOCUMENT INFORMATION
        // ==================================================

        public string? DocumentNumber { get; set; }

        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public bool IsExpired { get; set; }


        // ==================================================
        // FILE
        // ==================================================

        public string FileName { get; set; } = default!;

        public string FileUrl { get; set; } = default!;

        public string PublicId { get; set; } = default!;


        // ==================================================
        // VERIFICATION
        // ==================================================

        public string Status { get; set; } = default!;

        public Guid? VerifiedBy { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? Remarks { get; set; }


        // ==================================================
        // AI PARSING
        // ==================================================

        public string? DetectedDocumentType { get; set; }

        // Original database value.
        //
        // Example:
        // 0.98
        //
        // This is kept if other parts of your application
        // already use it.


        // ==================================================
        // AI EXTRACTION PERCENTAGE
        // ==================================================
        //
        // Document-wise percentage.
        //
        // Example:
        //
        // 0.98 -> 98
        // 0.75 -> 75
        //
        public decimal? AiExtractionPercentage { get; set; }


        // ==================================================
        // DOCUMENT VERIFICATION SETTINGS
        // ==================================================

        public bool RequiresVerification { get; set; }

        public bool IsMandatory { get; set; }
    }
}