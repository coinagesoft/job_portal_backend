using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class UpdateRecruiterStatusRequestDto
    {
        public string Status { get; set; } = default!;
        public string? Reason { get; set; }
    }
}
