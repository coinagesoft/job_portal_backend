using JobPortal.Domain.Enums.RecruiterEnums;
using System;

namespace JobPortal.Application.DTOs.Recruiter.CompanyDocuments
{
    public class RecruiterDocumentTypeDto
    {
        public Guid DocumentTypeId { get; set; }
        public string DocumentName { get; set; } = default!;
        public string Category { get; set; } = default!;
        public bool IsMandatory { get; set; }
        public bool RequiresVerification { get; set; }
        public bool AllowMultipleUploads { get; set; }
        public int DisplayOrder { get; set; }
        public string? Description { get; set; }

        // Null when this employer hasn't uploaded anything against this type yet.
        public Guid? MyDocumentId { get; set; }
        public VerificationDocumentStatus? MyStatus { get; set; }
        public DateTime? MyUploadedAt { get; set; }
    }
}
