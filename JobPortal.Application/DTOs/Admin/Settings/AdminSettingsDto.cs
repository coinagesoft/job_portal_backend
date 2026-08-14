using System;

namespace JobPortal.Application.DTOs.Admin.Settings
{
    /// <summary>Response for GET api/admin/settings.</summary>
    public class AdminSettingsDto
    {
        public string Language { get; set; } = default!;
        public string SessionTimeout { get; set; } = default!;
        public DateTime UpdatedAt { get; set; }
    }
}