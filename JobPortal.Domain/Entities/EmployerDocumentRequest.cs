using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
  
        public class EmployerDocumentRequest
        {
            [Key]
            public Guid RequestId { get; set; }

            // Recruiter who needs to upload the document
            public Guid EmployerId { get; set; }

            // Existing document type from VerificationDocumentMaster
            public Guid? DocumentTypeId { get; set; }

        // Used only when admin selects "Other".
        public string? CustomDocumentName { get; set; }

        // Message sent by admin to recruiter
        public string? Message { get; set; }

            // Pending / Uploaded / Cancelled
            public string Status { get; set; } = "Pending";

            // Admin who requested the document
            public Guid RequestedBy { get; set; }

            public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

            // Navigation
            public EmployerProfile Employer { get; set; } = default!;

            public VerificationDocumentMaster DocumentType { get; set; } = default!;


        [ForeignKey(nameof(RequestedBy))]
        public AdminUser RequestedByAdmin { get; set; } = default!;
    }
    }
