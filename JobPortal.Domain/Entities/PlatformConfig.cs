using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class PlatformConfig
{
    public Guid ConfigId { get; set; }
    public byte ReengagementIntervalDays { get; set; } = 30;
    public string ReengagementChannel { get; set; } = "Both";
    public string WhatsappTemplateId { get; set; } = default!;
    public bool FcmFallbackEnabled { get; set; } = true;
    public byte CvUnlockValidityDays { get; set; } = 60;
    public string WatermarkTemplate { get; set; } = "Unlocked by {company} on {date}";
    public string CreditExpiryAlertDays { get; set; } = default!; // JSON [30,15,7]
    public string AlertChannels { get; set; } = "Both";
    public byte TrialDurationDays { get; set; } = 14;
    public byte TrialFreeCredits { get; set; } = 5;
    public bool TrialCvDownloadAllowed { get; set; } = false;
    public bool OneTrialPerGstDomain { get; set; } = true;
    public Guid UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AdminUser UpdatedByAdmin { get; set; } = default!;
}
