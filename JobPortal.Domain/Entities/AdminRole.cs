using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{

    public class AdminRole
    {
        public Guid RoleId { get; set; }

        public string RoleName { get; set; } = default!;

        public string? Description { get; set; }

        // JSON Permissions
        public string Permissions { get; set; } = "{}";

        // System Role?
        public bool IsSystemRole { get; set; } = true;

        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<AdminUser> AdminUsers { get; set; } = new List<AdminUser>();
    }
}
