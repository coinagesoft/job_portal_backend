using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{

    public class PurchaseHistoryDto
    {
        public Guid PurchaseId { get; set; }

        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public DateTime AssignedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; }
    }
}
