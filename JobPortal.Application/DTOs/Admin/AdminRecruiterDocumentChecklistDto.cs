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


        public string DocumentName { get; set; } = default!;


        public bool IsCommonDocument { get; set; }


        public string Status { get; set; } = "Pending";

        public Guid DocumentId { get; set; }


        public DateTime UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }
    }
}
