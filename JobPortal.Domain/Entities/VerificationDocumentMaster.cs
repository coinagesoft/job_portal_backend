using JobPortal.Domain.Entities;
using System.ComponentModel.DataAnnotations;

public class VerificationDocumentMaster
{
    [Key]
    public Guid DocumentTypeId { get; set; }

    public string Code { get; set; } = default!;

    public string DocumentName { get; set; } = default!;

    public string Category { get; set; } = default!;

    // Company Registration = true
    // GST = false
    // PAN = false
    public bool IsMandatory { get; set; }

    public bool IsActive { get; set; } = true;

    public bool RequiresVerification { get; set; } = true;

    // System document or custom document
    public bool IsSystemDocument { get; set; }

    // Can employer upload multiple files?
    public bool AllowMultipleUploads { get; set; }

    // Admin can allow "Other"
    public bool AllowCustomDocument { get; set; }

    public int DisplayOrder { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmployerVerificationDocument> EmployerDocuments
        = new List<EmployerVerificationDocument>();
}