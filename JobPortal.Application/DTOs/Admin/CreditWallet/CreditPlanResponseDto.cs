using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.CreditWallet
{
    public class CreditPlanResponseDto
    {
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = default!;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public int ValidityMonths { get; set; }

        public bool IsActive { get; set; }
    }
}
