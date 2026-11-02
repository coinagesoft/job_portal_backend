using JobPortal.Domain.Enums.common;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.MembershipPlan
{
    public class CreateMembershipPlanRequestDto
    {
        public PlanType PlanType { get; set; }

        public string Region { get; set; } = "us";

        public string PlanName { get; set; } = default!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string Period { get; set; } = "one-time";

        // Credits added to the recruiter wallet on purchase
        public int InitialCredits { get; set; } = 0;

        // Validity of those credits, in months
        public int ValidityMonths { get; set; } = 0;

        public string? Badge { get; set; }

        public List<string> Features { get; set; } = new();
    }
}