using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{
    public class OptionalDocumentTypeDto
    {
        public Guid DocumentTypeId { get; set; }
        public string DocumentName { get; set; } = default!;
    }
}
