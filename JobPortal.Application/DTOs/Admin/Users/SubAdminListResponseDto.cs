namespace JobPortal.Application.DTOs.Admin.Users
{
    public class SubAdminListResponseDto
    {
        public bool Success { get; set; } = true;

        public string? Message { get; set; }

        // Only rows matching the current search/status filter, for this page.
        public List<SubAdminDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        // Unfiltered counts across ALL sub-admins (not just this page/filter)
        // for the "Total Users" / "Active" / "Suspended" stat cards. The
        // Super Admin is intentionally not included here — the frontend
        // adds it in separately (see DEFAULT_SUPER_ADMIN in
        // src/app/admin/users/page.js).
        public int TotalSubAdmins { get; set; }

        public int ActiveCount { get; set; }

        public int SuspendedCount { get; set; }
    }
}