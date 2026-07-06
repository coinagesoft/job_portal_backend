using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class GetPreferenceSettingsResponseDto
    {
        public string PrimaryLanguage { get; set; } = default!;

        public string? SecondaryLanguage { get; set; }

        public int ItemsPerPage { get; set; }

        public string DateFormat { get; set; } = default!;

        public bool MarketingEmailsEnabled { get; set; }

        public bool PlatformUpdatesEnabled { get; set; }
    }
}
