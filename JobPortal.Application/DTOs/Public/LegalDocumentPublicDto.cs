using System;

namespace JobPortal.Application.DTOs.Public
{
    /// <summary>
    /// What candidates/employers see when viewing the Privacy Policy or
    /// Terms &amp; Conditions page. Only ever reflects the last published
    /// version — drafts are never exposed publicly.
    /// </summary>
    public class LegalDocumentPublicDto
    {
        /// <summary>"privacy" | "terms"</summary>
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public DateTime EffectiveDate { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}