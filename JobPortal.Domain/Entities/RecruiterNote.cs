// ============================================================
//  JobPortal.Domain/Entities/RecruiterNote.cs
// ============================================================

namespace JobPortal.Domain.Entities;

public class RecruiterNote
{
    public Guid RecruiterNoteId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid EmployerId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsAcknowledged { get; set; } = false;
    public DateTime? AcknowledgedAt { get; set; }

    // Navigation
    public JobApplication JobApplication { get; set; } = default!;
    public EmployerProfile EmployerProfile { get; set; } = default!;
}