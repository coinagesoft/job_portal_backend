using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class SubUserListItemDto
    {
        public Guid SubUserId { get; set; }

        public Guid EmployerId { get; set; }
        public string SubUserName { get; set; } = string.Empty;
        public string SubUserEmail { get; set; } = string.Empty;
        public string SubUserMobile { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;      // Active | Pending | Deactivated
        public bool InviteAccepted { get; set; }
        public PermissionsDto Permissions { get; set; } = default!;
        public DateTime CreatedAt { get; set; }

        // Credit allocation — 0/0/0 if the owner has never allocated
        // credits to this sub-user yet.
        public int AllocatedCredits { get; set; }
        public int UsedCredits { get; set; }
        public int RemainingCredits { get; set; }
    }

    public class SubUserListResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SubUserListItemDto> SubUsers { get; set; } = new();
        public int TotalCount { get; set; }
    }
}