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

        // Page/feature access keys selected on the drawer
        // (e.g. "dashboard", "candidates.view", "subadmin.create" ...).
        [Required(ErrorMessage = "At least one permission must be selected.")]
        [MinLength(1, ErrorMessage = "At least one permission must be selected.")]
        public List<string> Permissions { get; set; } = new();

        // Maps to the "Account Status" select (Active/Suspended). Defaults
        // to Active — matches a freshly created sub-admin being usable
        // right away since login is OTP-based (no password to set up).
        public bool IsActive { get; set; } = true;
    }
}