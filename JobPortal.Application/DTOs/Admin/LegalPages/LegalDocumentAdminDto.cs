using System;

namespace JobPortal.Application.DTOs.Admin.LegalPages
{
    /// <summary>
    /// Full editor state for one legal document (Privacy Policy / Terms &amp; Conditions),
    /// matching what the "Legal Pages" admin screen needs: the draft being edited plus
    /// the last published version and its status.
    /// </summary>
    public class LegalDocumentAdminDto
    {
        public Guid DocumentId { get; set; }

        /// <summary>"privacy" | "terms"</summary>
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;

        public string DraftContent { get; set; } = default!;
        public DateTime? DraftEffectiveDate { get; set; }

        public string? PublishedContent { get; set; }
        public DateTime? PublishedEffectiveDate { get; set; }
        public DateTime? PublishedAt { get; set; }

        /// <summary>Draft | Published</summary>
        public string Status { get; set; } = default!;

        /// <summary>True when the draft differs from what's currently published.</summary>
        public bool HasUnpublishedChanges { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}