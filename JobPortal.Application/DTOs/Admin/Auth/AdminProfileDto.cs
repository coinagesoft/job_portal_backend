using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{

    public class AdminProfileDto
    {
        public Guid AdminId { get; set; }

        public Guid UserId { get; set; }

        public string AdminIdentifier { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string AdminType { get; set; } = string.Empty;

        public Guid RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
