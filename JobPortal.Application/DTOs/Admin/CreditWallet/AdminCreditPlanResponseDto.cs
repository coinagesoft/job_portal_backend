using System;

namespace JobPortal.Application.DTOs.Admin.CreditWallet
{
    // Used only by the Admin credit-plan GET endpoints. Deliberately
    // excludes Region and Bonus — the admin create/update forms don't
    // collect either value, so surfacing them here was just showing
    // meaningless defaults (bug #50). Region/Bonus are still used
    // internally (see CreditPlan entity) and on the recruiter-facing
    // CreditPlanResponseDto, which this does NOT replace.
    public class AdminCreditPlanResponseDto
    {
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = default!;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public int ValidityMonths { get; set; }

        public bool IsActive { get; set; }
    }
}