using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Recruiters by Industry" donut chart on Admin ▸ Dashboard.
    // GET /api/admin/dashboard/recruiters-by-industry
    public class RecruitersByIndustryResponseDto
    {
        public int TotalRecruiters { get; set; }

        // Top industries by recruiter count, largest first. Anything
        // outside the top slices is folded into a trailing "Other"
        // entry so the chart never shows more than a handful of
        // segments (mirrors the donut on the dashboard page).
        public List<IndustrySliceDto> Slices { get; set; } = new();
    }

    public class IndustrySliceDto
    {
        public string Industry { get; set; } = default!;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}