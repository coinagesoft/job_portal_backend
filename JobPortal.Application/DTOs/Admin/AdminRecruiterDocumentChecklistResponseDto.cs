using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistResponseDto
    {
        public Guid EmployerId { get; set; }

        // Number of items displayed in checklist
        // Includes common + additional documents
        public int Total { get; set; }

        // Verification is ONLY based on common documents
        public int VerificationTotal { get; set; }

        public int Verified { get; set; }

        public int NotUploaded { get; set; }

        public int Rejected { get; set; }

        public int VerificationPercentage { get; set; }

        public string VerificationStatus { get; set; } = "Pending";

        public List<AdminRecruiterDocumentChecklistDto> Documents { get; set; }
            = new();
    }
}
