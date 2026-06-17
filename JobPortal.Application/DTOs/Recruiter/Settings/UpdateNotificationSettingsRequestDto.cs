using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class UpdateNotificationSettingsRequestDto
    {
        public bool? PrefEmailEnabled { get; set; }

        public bool? PrefPushEnabled { get; set; }

        public bool? PrefApplicantNotify { get; set; }

        public bool? PrefCreditExpiryEmail { get; set; }

        public bool? PrefJobStatusUpdates { get; set; }

        public bool? PrefSystemMessages { get; set; }

        public short? SessionTimeoutMinutes { get; set; }
    }
}
