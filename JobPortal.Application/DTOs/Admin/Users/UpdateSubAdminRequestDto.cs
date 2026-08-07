using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Admin.Users
{
    // Backs the "Edit Sub Admin" drawer on /admin/users.
    // PUT /api/admin/sub-admins/{id}
    // Email is intentionally not editable here — it's the sub-admin's
    // login identity; changing it would need its own re-verification flow.
    public class UpdateSubAdminRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        // Optional — matches the "Phone Number" field on the drawer.
        public string? MobileNumber { get; set; }

        public string? CountryCode { get; set; } = "+91";

        // Role label shown on the drawer, e.g. "Verification Officer",
        // "Finance Admin", "Employer Manager", "Read Only" or "Custom".
        [Required(ErrorMessage = "Role is required.")]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        // Page/feature access keys selected on the drawer.
        [Required(ErrorMessage = "At least one permission must be selected.")]
        [MinLength(1, ErrorMessage = "At least one permission must be selected.")]
        public List<string> Permissions { get; set; } = new();

        // Maps to the "Account Status" select (Active/Suspended).
        public bool IsActive { get; set; } = true;
    }
}