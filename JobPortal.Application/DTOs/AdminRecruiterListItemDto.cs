using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{

    public class AdminRecruiterListItemDto
    {
            public string Id { get; set; } = default!;
            public string? Logo { get; set; }
            public string Company { get; set; } = default!;
            public string? Sector { get; set; }
            public string Person { get; set; } = default!;
            public string? Email { get; set; }
            public string Plan { get; set; } = default!;
            public string Gst { get; set; } = default!;
            public int DocsVerified { get; set; }
            public int DocsTotal { get; set; }
            public string Status { get; set; } = default!;
            public string Registered { get; set; } = default!;
    }
}
