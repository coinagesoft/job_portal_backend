using System;

namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{
    public class PendingCompanyDocumentDto
    {
        public Guid DocumentId { get; set; }
        public Guid EmployerId { get; set; }
        public string CompanyName { get; set; } = default!;

        public string DocumentName { get; set; } = default!;
        public string Category { get; set; } = default!;

        public string FileUrl { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
    }
}
