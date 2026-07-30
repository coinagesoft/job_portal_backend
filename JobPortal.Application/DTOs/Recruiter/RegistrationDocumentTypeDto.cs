using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationDocumentTypeDto
    {
        public Guid DocumentTypeId { get; set; }

        public string DocumentName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public bool IsMandatory { get; set; }

        public bool AllowMultipleUploads { get; set; }

        public bool AllowCustomDocument { get; set; }

        public bool RequiresVerification { get; set; }

        public int DisplayOrder { get; set; }
    }
}
