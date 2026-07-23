using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{
    public class CreateDocumentTypeRequestDto
    {
        public string DocumentName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public bool IsMandatory { get; set; }

        public bool RequiresVerification { get; set; } = true;
    }
}
