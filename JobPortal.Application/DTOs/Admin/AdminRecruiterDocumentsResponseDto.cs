using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // Only common/admin-created documents
            public int Total { get; set; }

            public int Verified { get; set; }

            public int NotUploaded { get; set; }

            public int Rejected { get; set; }

            public int VerificationPercentage { get; set; }

            public string Status { get; set; } = "Pending";
        }

        public class AdminRecruiterDocumentDto
        {
            public Guid DocumentId { get; set; }

            public Guid? DocumentTypeId { get; set; }

            // true = linked with VerificationDocumentMaster
            // false = additional/custom document
            public bool IsCommonDocument { get; set; }

            public string DocumentName { get; set; } = default!;

            public string? Category { get; set; }

            public string? DocumentNumber { get; set; }

            public string? IssuingAuthority { get; set; }

            public DateOnly? IssueDate { get; set; }

            public DateOnly? ExpiryDate { get; set; }

            public bool IsExpired { get; set; }

            public string FileName { get; set; } = default!;

            public string FileUrl { get; set; } = default!;

            public string PublicId { get; set; } = default!;

            public string Status { get; set; } = default!;

            public Guid? VerifiedBy { get; set; }

            public DateTime UploadedAt { get; set; }

            public DateTime? VerifiedAt { get; set; }

            public string? Remarks { get; set; }

            public string? DetectedDocumentType { get; set; }

            public decimal? AiConfidenceScore { get; set; }


            public bool RequiresVerification { get; set; }

            public bool IsMandatory { get; set; }
        }
    }
