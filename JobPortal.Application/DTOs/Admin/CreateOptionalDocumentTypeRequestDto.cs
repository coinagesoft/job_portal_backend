using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class CreateOptionalDocumentTypeRequestDto
    {
        public string DocumentName { get; set; } = default!;

        public string Category { get; set; } = default!;

    }
}
