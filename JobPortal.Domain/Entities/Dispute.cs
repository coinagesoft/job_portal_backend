using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class Dispute
{
    public Guid DisputeId { get; set; }
    public Guid RaisedBy { get; set; }
    public string DisputeType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Status { get; set; } = "Open";
    public Guid? AssignedTo { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public User RaisedByUser { get; set; } = default!;
    public AdminUser? AssignedAdmin { get; set; }
}
