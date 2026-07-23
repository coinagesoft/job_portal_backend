using System;

namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{
    public class DocumentTypeAdminDto
    {
        public Guid Id { get; set; }
        public string DocumentName { get; set; } = default!;
        public string Category { get; set; } = default!;
        public bool IsMandatory { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresVerification { get; set; }
        public bool AllowMultipleUploads { get; set; }
        public int DisplayOrder { get; set; }
        public string? Description { get; set; }
    }
}
