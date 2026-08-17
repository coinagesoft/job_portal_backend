using System;

namespace JobPortal.Application.DTOs.Admin.CreditWallet
{
    // Used only by the Admin credit-plan GET endpoints.
    public class AdminCreditPlanResponseDto
    {
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = default!;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public int ValidityMonths { get; set; }

        // Pricing-region code, e.g. "us", "in", "ae". The admin
        // create/update forms collect this (CreateCreditPlanRequestDto /
        // UpdateCreditPlanRequestDto), so it belongs in the response too.
        public string Region { get; set; } = default!;

        public string? Bonus { get; set; }

        public bool IsActive { get; set; }
    }
}