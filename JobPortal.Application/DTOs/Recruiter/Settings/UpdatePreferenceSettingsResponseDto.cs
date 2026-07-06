using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class UpdatePreferenceSettingsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;
    }
}
