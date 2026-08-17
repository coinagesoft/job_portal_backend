using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Revenue
{
    public class RevenueCountryRowDto
    {
        public string Country { get; set; } = default!;

        // Short display code for the country badge, e.g. "USA", "IND".
        public string CountryCode { get; set; } = default!;

        public decimal Amount { get; set; }

        public decimal PercentOfTotal { get; set; }
    }

    // Composition of revenue across the 3 transaction categories,
    // shown in the "period panel" doughnut/legend on the right.
    public class RevenueCompositionDto
    {
        public decimal CandidatePercent { get; set; }
        public decimal RecruiterPercent { get; set; }
        public decimal CreditsPercent { get; set; }
    }

    public class RevenueByCountryDto
    {
        // "monthly" or "yearly" — echoes back the requested period.
        public string Period { get; set; } = "monthly";

        public decimal TotalAmount { get; set; }

        public List<RevenueCountryRowDto> Countries { get; set; } = new();

        public RevenueCompositionDto Composition { get; set; } = new();
    }
}