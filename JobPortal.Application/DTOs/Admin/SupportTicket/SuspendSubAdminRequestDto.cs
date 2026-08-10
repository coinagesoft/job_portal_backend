using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Admin.Users
{
    // Backs the "Suspend" quick-action on /admin/users (row-level toggle,
    // as opposed to the full "Edit Sub Admin" drawer).
    // PATCH /api/admin/sub-admins/{id}/suspend
    public class SuspendSubAdminRequestDto
    {
        // Optional — shown against the account if/when it's looked up
        // later (e.g. "Suspended: Policy violation").
        [MaxLength(250)]
        public string? Reason { get; set; }
    }
}