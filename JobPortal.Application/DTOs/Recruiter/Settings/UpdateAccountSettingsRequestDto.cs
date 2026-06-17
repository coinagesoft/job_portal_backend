using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class UpdateAccountSettingsRequestDto
    {
        public string? ContactPersonName { get; set; } = default!;

        public string? Designation { get; set; } = default!;

        public string? TimeZone { get; set; } = default!;

        public string? Email { get; set; } = default!;

        public string? MobileNumber { get; set; } = default!;

        public string? CountryCode { get; set; } = default!;
    }
}
