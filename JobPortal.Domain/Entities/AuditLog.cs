using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class AuditLog
{
    public Guid LogId { get; set; }
    public string ActionType { get; set; } = default!;
    public Guid PerformedBy { get; set; }
    public string PerformedByName { get; set; } = default!;  // snapshot
    public string TargetEntityType { get; set; } = default!;
    public Guid TargetEntityId { get; set; }
    public string ActionDetail { get; set; } = default!;     // JSON
    public string? ChangeReason { get; set; }
    public string IpAddress { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public AdminUser PerformedByAdmin { get; set; } = default!;
}
