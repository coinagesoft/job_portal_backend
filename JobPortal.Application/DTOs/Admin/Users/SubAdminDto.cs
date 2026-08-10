namespace JobPortal.Application.DTOs.Admin.Users
{
    public class SubAdminDto
    {
        public Guid AdminId { get; set; }

        public Guid UserId { get; set; }

        public string AdminIdentifier { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? MobileNumber { get; set; }

        // "SuperAdmin" | "SubAdmin"
        public string AdminType { get; set; } = string.Empty;

        public Guid RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        // Same true/false-per-tab shape as the request DTOs, so a client
        // can take a GET/Create/Update response and echo it straight back
        // into the Edit drawer's request body without reshaping it.
        public SubAdminPermissionsDto Permissions { get; set; } = new();

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Null if the sub-admin has never logged in ("Never" on the list).
        public DateTime? LastLoginAt { get; set; }
    }
}