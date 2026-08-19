using System;

namespace JobPortal.Application.DTOs.Admin
{
    public class CompanyRequiredDocumentVerificationDto
    {
        // ==================================================
        // IDS
        // ==================================================

        public Guid DocumentId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        public Guid? RequestId { get; set; }


        // ==================================================
        // DOCUMENT NAME
        // ==================================================

        public string DocumentName { get; set; } = string.Empty;


        // ==================================================
        // DOCUMENT TYPE
        // ==================================================
        //
        // Example:
        // GST
        // PAN
        // Company Registration
        // Other
        //

        public string? DocumentType { get; set; }


        // ==================================================
        // DOCUMENT CATEGORY
        // ==================================================
        //
        // Mandatory
        // Optional
        // RequestedAdditional
        //

        public string DocumentCategory { get; set; } = string.Empty;


        // ==================================================
        // DOCUMENT TYPE CATEGORY
        // ==================================================
        //
        // Example:
        // Tax
        // License
        // Registration
        // Identity
        // Other
        //

        public string? DocumentTypeCategory { get; set; }


        // ==================================================
        // DOCUMENT VERIFICATION STATUS
        // ==================================================
        //
        // Pending
        // Approved
        // Rejected
        // Expired
        // Resubmission
        //

        public string DocumentVerificationStatus { get; set; }
            = string.Empty;
    }
}