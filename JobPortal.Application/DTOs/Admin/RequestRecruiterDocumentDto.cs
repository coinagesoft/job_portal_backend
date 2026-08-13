using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class RequestRecruiterDocumentDto
    {
        public Guid? DocumentTypeId { get; set; }

        public string? CustomDocumentName { get; set; }

        public string? Message { get; set; }
    }
}
