using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.CreditWallet
{
    public class CommonResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        // Populated by Create endpoints so callers (e.g. the admin
        // Plans page) can immediately reference the new record
        // without a follow-up lookup.
        public Guid? PlanId { get; set; }
    }
}