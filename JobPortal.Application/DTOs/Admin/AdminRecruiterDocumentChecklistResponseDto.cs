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
        //
        // Mandatory
        // Optional
        // Additional
        // RequestedAdditional
        //
        public int Total { get; set; }


        // ==================================================
        // DOCUMENTS REQUIRING VERIFICATION
        // ==================================================
        //
        // Mandatory
        // Optional
        // RequestedAdditional
        //
        public int VerificationTotal { get; set; }


        // ==================================================
        // VERIFICATION COUNTS
        // ==================================================

        // Uploaded and Approved
        public int Verified { get; set; }


        // Uploaded but waiting for admin verification
        public int Pending { get; set; }


        // Required for verification but not uploaded
        public int NotUploaded { get; set; }


        // Uploaded but rejected
        public int Rejected { get; set; }


        // ==================================================
        // VERIFICATION STATUS
        // ==================================================

        public string VerificationStatus { get; set; } = "Pending";


        // ==================================================
        // DOCUMENTS
        // ==================================================

        public List<AdminRecruiterDocumentChecklistDto> Documents { get; set; }
            = new();
    }
}