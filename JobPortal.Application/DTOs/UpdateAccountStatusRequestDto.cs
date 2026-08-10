using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{
    public class UpdateAccountStatusRequestDto
    {
        public string AccountStatus { get; set; } = default!;
        public string? Reason { get; set; }
    }
}
