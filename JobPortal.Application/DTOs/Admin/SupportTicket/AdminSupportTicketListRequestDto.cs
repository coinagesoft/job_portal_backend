using JobPortal.Domain.Enums.RecruiterEnums;

namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    // Backs the tabs + filter bar + table on /admin/helpAndsupport.
    // GET /api/admin/support-tickets?raisedByType=&status=&category=&search=&page=&pageSize=
    public class AdminSupportTicketListRequestDto
    {
        // "Candidate" | "Recruiter" (also accepts "Employer" as a synonym
        // for Recruiter). Omit/blank to return tickets from both sides —
        // used when the frontend needs an "All" view.
        public string? RaisedByType { get; set; }

        // "Open" | "InProgress" | "Resolved"
        public string? Status { get; set; }

        public SupportTicketType? Category { get; set; }

        // Free-text search over subject, description and the raiser's name.
        public string? Search { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}