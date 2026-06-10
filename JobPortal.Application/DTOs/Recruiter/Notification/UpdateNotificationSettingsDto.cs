using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Notification
{
    public class UpdateNotificationSettingsDto
    {
        public bool NewApplicantAlerts { get; set; }

        public bool CreditBillingAlerts { get; set; }

        public bool JobStatusUpdates { get; set; }

        public bool SystemMessages { get; set; }
    }
}
