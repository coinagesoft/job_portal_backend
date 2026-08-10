using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class UpdateRecruiterDocumentStatusRequestDto
    {
        public string Status { get; set; } = default!;

        public string? Remarks { get; set; }
    }
}
