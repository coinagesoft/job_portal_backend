using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class PermissionDto
    {
        public string Module { get; set; } = string.Empty;

        public bool CanView { get; set; }

        public bool CanCreate { get; set; }

        public bool CanUpdate { get; set; }

        public bool CanDelete { get; set; }

        public bool CanApprove { get; set; }

        public bool CanExport { get; set; }
    }
}
