using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistResponseDto
    {
        public Guid EmployerId { get; set; }

        // ==================================================
        // ALL DOCUMENTS
        // ==================================================
        // Mandatory
        // Optional
        // Additional
        // RequestedAdditional
        public int Total { get; set; }


        // ==================================================
        // DOCUMENTS REQUIRING VERIFICATION
        // ==================================================
        // Mandatory
        // Optional where RequiresVerification = true
        // RequestedAdditional
        //
        // Normal Additional documents are excluded.
        public int VerificationTotal { get; set; }


        // ==================================================
        // VERIFIED
        // ==================================================
        // Documents requiring verification
        // whose status is Approved.
        public int Verified { get; set; }


        // ==================================================
        // NOT UPLOADED
        // ==================================================
        // Documents requiring verification
        // for which no file has been uploaded.
        //
        // Status = NotUploaded
        public int NotUploaded { get; set; }


        // ==================================================
        // PENDING
        // ==================================================
        // Documents that have been uploaded but are
        // waiting for admin verification.
        //
        // Status = Pending
        public int Pending { get; set; }


        // ==================================================
        // REJECTED
        // ==================================================
        // Documents requiring verification
        // whose status is Rejected.
        public int Rejected { get; set; }


        // ==================================================
        // OVERALL VERIFICATION STATUS
        // ==================================================
        //
        // Pending
        // Verified
        // Rejected
        public string VerificationStatus { get; set; } = "Pending";


        // ==================================================
        // DOCUMENT CHECKLIST
        // ==================================================

        public List<AdminRecruiterDocumentChecklistDto> Documents { get; set; }
            = new();
    }
}