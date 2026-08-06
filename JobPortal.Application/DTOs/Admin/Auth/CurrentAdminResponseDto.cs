using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class CurrentAdminResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public AdminProfileDto? Admin { get; set; }

        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
