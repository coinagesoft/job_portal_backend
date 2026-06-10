using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class EmployerNotificationSetting
{
    public Guid NotifPrefId { get; set; }

    public Guid EmployerId { get; set; }

    public bool PrefEmailEnabled { get; set; } = true;

    public bool PrefPushEnabled { get; set; } = true;

    public bool PrefApplicantNotify { get; set; } = true;

    public bool PrefCreditExpiryEmail { get; set; } = true;

    public bool PrefJobStatusUpdates { get; set; } = true;

    public bool PrefSystemMessages { get; set; } = true;

    public string? FcmToken { get; set; }

    public short SessionTimeoutMinutes { get; set; } = 30;


    public EmployerProfile EmployerProfile { get; set; } = default!;
}
