using System;

namespace JobPortal.Domain.Entities;

/// <summary>
/// A legal/CMS document managed by the admin (Legal Pages screen) and
/// shown to candidates/employers on the public site — e.g. Privacy Policy,
/// Terms &amp; Conditions.
///
/// Draft* fields hold whatever the admin is currently editing.
/// Published* fields hold the last version that was actually published
/// and are the only fields the public API exposes.
/// </summary>
public class LegalDocument
{
    public Guid DocumentId { get; set; }

    /// <summary>Stable slug identifying the document, e.g. "privacy", "terms".</summary>
    public string Type { get; set; } = default!;

    public string Title { get; set; } = default!;

    // ── Draft (working copy shown/edited in the admin editor) ──
    public string DraftContent { get; set; } = default!;
    public DateTime? DraftEffectiveDate { get; set; }

    // ── Published (live copy served to candidates/employers) ──
    public string? PublishedContent { get; set; }
    public DateTime? PublishedEffectiveDate { get; set; }
    public DateTime? PublishedAt { get; set; }

    /// <summary>Draft | Published — Draft means there are unpublished changes.</summary>
    public string Status { get; set; } = "Draft";

    public Guid? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AdminUser? UpdatedByAdmin { get; set; }
}