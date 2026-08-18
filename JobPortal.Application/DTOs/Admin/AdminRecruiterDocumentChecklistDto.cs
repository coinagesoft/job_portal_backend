using System;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistDto
    {
        // ==================================================
        // DOCUMENT TYPE
        // ==================================================

        public Guid? DocumentTypeId { get; set; }

        public string DocumentName { get; set; } = default!;


        // ==================================================
        // BUSINESS CATEGORY
        // ==================================================
        //
        // Examples:
        // Tax
        // License
        // Registration
        // Identity
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
        public string DocumentCategory { get; set; } = "Additional";


        // ==================================================
        // DOCUMENT RULES
        // ==================================================

        public bool IsMandatory { get; set; }

        public bool RequiresVerification { get; set; }


        // ==================================================
        // STATUS
        // ==================================================
        //
        // NotUploaded
        // Pending
        // Approved
        // Rejected
        // Expired
        // Resubmission
        //
        public string Status { get; set; } = "NotUploaded";


        // ==================================================
        // REQUEST MESSAGE
        // ==================================================
        //
        // Only populated for RequestedAdditional documents.
        //
        // Mandatory    -> null
        // Optional     -> null
        // Additional   -> null
        // Requested    -> admin's message
        //
        public string? Message { get; set; }


        // ==================================================
        // UPLOADED DOCUMENT
        // ==================================================

        public Guid DocumentId { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }
    }
}