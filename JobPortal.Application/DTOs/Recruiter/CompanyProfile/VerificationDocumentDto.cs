using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class VerificationDocumentDto
    {
        public string DocumentType { get; set; } = default!;

        public Guid? DocumentId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        public Guid? RequestId { get; set; }
        public string? Category { get; set; }
        public string? FileUrl { get; set; }
        public string? Message { get; set; }
        public string DocumentName { get; set; } = string.Empty;

        public string? DocumentTypeCategory { get; set; }
        public string Status { get; set; } = default!;

        public DateTime? UploadedAt { get; set; }
    }
}
