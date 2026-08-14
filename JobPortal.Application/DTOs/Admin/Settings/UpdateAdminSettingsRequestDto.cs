using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Admin.Settings
{
    /// <summary>Body for PUT api/admin/settings.</summary>
    public class UpdateAdminSettingsRequestDto
    {
        [Required]
        public string Language { get; set; } = default!;

        [Required]
        public string SessionTimeout { get; set; } = default!;
    }
}