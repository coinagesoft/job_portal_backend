using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Domain.Entities
{
    // Admin-managed lifetime membership plan for either Recruiters
    // (employers) or Candidates. Priced per pricing Region so the
    // same plan concept can carry different prices for US, IN, Gulf
    // countries, etc.
    public class MembershipPlan
    {
        [Key]
        public Guid PlanId { get; set; }

        public PlanType PlanType { get; set; }

        // Pricing-region code, e.g. "us", "in", "ae" — matches the
        // region ids used on the admin Plans page.
        public string Region { get; set; } = "us";

        public string PlanName { get; set; } = default!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        // "one-time" for lifetime plans; kept as a string so future
        // periods (monthly/yearly) don't require a schema change.
        public string Period { get; set; } = "one-time";

        public string? Badge { get; set; }

        public List<string> Features { get; set; } = new();

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}