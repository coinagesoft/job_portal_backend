using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Registration Growth" line chart on Admin ▸ Dashboard —
    // new candidate vs recruiter sign-ups over the selected range.
    // GET /api/admin/dashboard/registration-growth?range=week|month|year
    public class RegistrationGrowthResponseDto
    {
        // "week" | "month" | "year" — echoes back the resolved range.
        public string Range { get; set; } = "week";

        // X-axis labels, e.g. ["Mon".."Sun"] for week,
        // ["Jan".."Dec"] (last 12 months) for month,
        // ["2021".."2026"] for year.
        public List<string> Labels { get; set; } = new();

        // New candidate registrations per label bucket.
        public List<int> Candidates { get; set; } = new();

        // New recruiter registrations per label bucket.
        public List<int> Recruiters { get; set; } = new();
    }
}