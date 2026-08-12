using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminDocumentRequirementDto
    {
        public Guid Id { get; set; }

        public string DocumentName { get; set; } = default!;

        public string Category { get; set; } = default!;

        public bool IsMandatory { get; set; }

        public bool RequiresVerification { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }
    }
}
