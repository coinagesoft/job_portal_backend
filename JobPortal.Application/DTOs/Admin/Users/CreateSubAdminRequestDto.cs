using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Admin.Users
{
    public class CreateSubAdminRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        // Optional — matches the "Phone Number" field on the Add Sub Admin drawer.
        public string? MobileNumber { get; set; }

        public string? CountryCode { get; set; } = "+91";

        // Role label shown on the drawer, e.g. "Verification Officer",
        // "Finance Admin", "Employer Manager", "Read Only" or "Custom".
        [Required(ErrorMessage = "Role is required.")]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        // Sidebar tab access, one true/false toggle per tab — matches the
        // "Sidebar Tab Access" list on the Add Sub Admin drawer 1:1. Keys
        // are the canonical set from
        // JobPortal.Domain.Constants.AdminSidebarTabs, kept in sync with
        // the frontend's TABS list in src/app/admin/users/page.js. At
        // least one must be true (enforced in AdminUserService).
        [Required(ErrorMessage = "Permissions are required.")]
        public SubAdminPermissionsDto Permissions { get; set; } = new();

        // Maps to the "Account Status" select (Active/Suspended). Defaults
        // to Active — matches a freshly created sub-admin being usable
        // right away since login is OTP-based (no password to set up).
        public bool IsActive { get; set; } = true;
    }
}