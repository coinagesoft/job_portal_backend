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

        public string? Badge { get; set; }

        public List<string> Features { get; set; } = new();
    }
}