using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistResponseDto
    {
        public Guid EmployerId { get; set; }

        // All documents displayed in the checklist:
        // Mandatory + Optional + Additional + RequestedAdditional
        public int Total { get; set; }

        // All documents that require verification:
        // Mandatory + Optional + RequestedAdditional
        public int VerificationTotal { get; set; }

        // Documents requiring verification that are Approved
        public int Verified { get; set; }

        // Documents requiring verification that have not been uploaded
        public int NotUploaded { get; set; }

        // Documents requiring verification that are Rejected
        public int Rejected { get; set; }

        public int VerificationPercentage { get; set; }

        public string VerificationStatus { get; set; } = "Pending";

        public List<AdminRecruiterDocumentChecklistDto> Documents { get; set; }
            = new();
    }
}