using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class SupportTicket
{
    public Guid TicketId { get; set; }
    public Guid RaisedBy { get; set; }
    public SupportTicketType TicketType { get; set; }
    public string Subject { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public Guid? AssignedTo { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SupportTicketReply> Replies { get; set; }
       = new List<SupportTicketReply>();
    public User RaisedByUser { get; set; } = default!;
    public AdminUser? AssignedAdmin { get; set; }
}
