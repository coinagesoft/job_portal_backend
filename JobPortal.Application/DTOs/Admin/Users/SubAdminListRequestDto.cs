namespace JobPortal.Application.DTOs.Admin.Users
{
    // Backs the toolbar (search + status filter) and table pagination on
    // /admin/users.
    // GET /api/admin/sub-admins?search=&status=&page=&pageSize=
    public class SubAdminListRequestDto
    {
        // Free-text search over name, email and role name — matches the
        // "Search name, email, role..." input on the drawer's toolbar.
        public string? Search { get; set; }

        // "Active" | "Suspended". Omit/blank for the "All Statuses" option.
        public string? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}