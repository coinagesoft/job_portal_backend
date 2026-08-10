using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminRecruiterDocumentChecklistDto
    {
        public Guid? DocumentTypeId { get; set; }

        public string? Code { get; set; }

        public string DocumentName { get; set; } = default!;

        public string? Category { get; set; }

        public bool IsCommonDocument { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresVerification { get; set; }

        public string Status { get; set; } = "Pending";

        public Guid DocumentId { get; set; }

        public string? Remarks { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }
    }
}
