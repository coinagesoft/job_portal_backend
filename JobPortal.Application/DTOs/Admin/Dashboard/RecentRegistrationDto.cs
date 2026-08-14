using System;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Recent Registrations" table on Admin ▸ Dashboard.
    // GET /api/admin/dashboard/recent-registrations?limit=5
    public class RecentRegistrationDto
    {
        public Guid UserId { get; set; }

        // "candidate" | "recruiter"
        public string Type { get; set; } = default!;

        public string Name { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        // Set when Type == "candidate" — use to deep-link to
        // /admin/candidates/candidateDetails.
        public Guid? CandidateId { get; set; }

        // Set when Type == "recruiter" — use to deep-link to
        // /admin/recruiters/details.
        public Guid? EmployerId { get; set; }
    }
}