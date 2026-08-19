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
        // REQUEST
        // ==================================================
        //
        // Used when the document was requested by admin.
        //
        // Normal uploaded document -> null
        // Requested document       -> RequestId
        //
        public Guid? RequestId { get; set; }


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
        // VERIFICATION STATUS
        // ==================================================
        //
        // Pending
        // Approved
        // Rejected
        // Expired
        // Resubmission
        //
        public string Status { get; set; } = "Pending";


        // ==================================================
        // REQUEST MESSAGE / ADMIN REMARKS
        // ==================================================
        //
        // For requested documents, this can contain the
        // admin's request message.
        //
        // Rejected / Resubmission remarks can also be stored
        // separately in the document's Remarks field if needed.
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