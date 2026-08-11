using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class CreditPlan
    {
        [Key]
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = default!;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public int ValidityMonths { get; set; }

        // Pricing-region code, e.g. "us", "in", "ae". Defaults to "us"
        // so existing rows keep working after the migration.
        public string Region { get; set; } = "us";

        // Free-text bonus label shown to the admin/recruiter,
        // e.g. "50 bonus credits". Empty when there's no bonus.
        public string? Bonus { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }
    }
}