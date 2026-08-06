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

        public List<string> Permissions { get; set; } = new();

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}