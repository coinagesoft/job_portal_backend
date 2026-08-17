using System;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistDto
    {
        public Guid? DocumentTypeId { get; set; }

        public string DocumentName { get; set; } = default!;

        // --------------------------------------------------
        // DOCUMENT CATEGORY
        // --------------------------------------------------
        //
        // Possible values:
        //
        // Mandatory
        // Optional
        // Additional
        // RequestedAdditional
        //
        public string DocumentCategory { get; set; } = string.Empty;

        // --------------------------------------------------
        // MANDATORY
        // --------------------------------------------------
        //
        // true  = Mandatory
        // false = Optional / Non-Mandatory
        //
        public bool IsMandatory { get; set; }

        // --------------------------------------------------
        // VERIFICATION
        // --------------------------------------------------
        //
        // true  = Document participates in verification
        // false = Document does not participate
        //
        public bool RequiresVerification { get; set; }

        // --------------------------------------------------
        // DOCUMENT STATUS
        // --------------------------------------------------
        //
        // Possible values:
        //
        // NotUploaded
        // Pending
        // Approved
        // Rejected
        //
        public string Status { get; set; } = "NotUploaded";

        // --------------------------------------------------
        // UPLOADED DOCUMENT
        // --------------------------------------------------

        public Guid DocumentId { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }
    }
}