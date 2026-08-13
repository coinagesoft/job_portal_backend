using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class EmployerDocumentRequestDto
    {
        public Guid RequestId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        public string? CustomDocumentName { get; set; }

        public string DocumentName { get; set; } = default!;

        public string? Message { get; set; }

        public string Status { get; set; } = default!;

        public DateTime RequestedAt { get; set; }
    }
}
