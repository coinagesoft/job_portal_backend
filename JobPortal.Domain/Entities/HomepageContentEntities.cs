// ============================================================
//  JobPortal.Domain/Entities/Homepage/HomepageContentEntities.cs
//
//  Backs the Admin "Homepage Management" screen
//  (https://job-portal-admin-gray.vercel.app/admin/homepage-management)
//  and feeds the candidate-facing homepage
//  (https://job-portal-dev-phi.vercel.app/).
//
//  Kept in a single file on purpose — these are small, uniform
//  "content block" entities that are always read/edited together
//  from one admin screen, so one file is easier to navigate than
//  nine near-identical ones.
// ============================================================

using JobPortal.Domain.Entities;
using System;
using System.Collections.Generic;

namespace JobPortal.Domain.Entities.Homepage;

/// <summary>
/// Hero banner shown at the top of the candidate homepage.
/// Singleton — there is always exactly one row (seeded).
/// </summary>
public class HomepageHero
{
    public Guid HeroId { get; set; }

    public string Headline { get; set; } = default!;

    public string? Subheadline { get; set; }

    public string? SearchPlaceholder { get; set; }

    public string? CtaText { get; set; }

    public string? CtaLink { get; set; }

    public string? BannerImageUrl { get; set; }

    /// <summary>Storage key used to overwrite/delete the file on re-upload.</summary>
    public string? BannerImagePublicId { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public AdminUser? UpdatedByAdmin { get; set; }
}

/// <summary>"Browse by Industry" tiles on the homepage.</summary>
public class HomepageIndustry
{
    public Guid IndustryId { get; set; }

    public string Name { get; set; } = default!;

    public string? Slug { get; set; }

    public string? IconUrl { get; set; }

    /// <summary>
    /// Optional manual override for the job count badge. When null the
    /// candidate site should compute it live from active job postings.
    /// </summary>
    public int? JobCountOverride { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Whether this industry also appears in the registration/search dropdown.</summary>
    public bool ShowInDropdown { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// The "Hiring Statistics" strip (e.g. Active Jobs / Companies / Candidates
/// / Placements). Singleton — the whole list of stat cards is replaced in
/// one PUT, so there's no separate add/delete endpoint for individual cards.
/// </summary>
public class HomepageStatistics
{
    public Guid StatisticsId { get; set; }

    public List<HomepageStatItem> Items { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}

/// <summary>One stat card inside <see cref="HomepageStatistics"/>. Stored as JSON, not its own table.</summary>
public class HomepageStatItem
{
    public string Label { get; set; } = default!;

    public string Value { get; set; } = default!;

    /// <summary>e.g. "+", "K", "%" — appended after the value on the UI.</summary>
    public string? Suffix { get; set; }

    public string? IconSlug { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>"Browse Jobs by Location" tiles on the homepage.</summary>
public class HomepageLocation
{
    public Guid LocationId { get; set; }

    public string Name { get; set; } = default!;

    public string? Country { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public int? JobCountOverride { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>"Browse Jobs by Role" tiles on the homepage.</summary>
public class HomepageRole
{
    public Guid RoleId { get; set; }

    public string Name { get; set; } = default!;

    public string? IconUrl { get; set; }

    public string? IconPublicId { get; set; }

    public int? JobCountOverride { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Industry options offered on the candidate/recruiter registration forms.</summary>
public class HomepageRegistrationIndustry
{
    public Guid RegistrationIndustryId { get; set; }

    public string Name { get; set; } = default!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Department options used across job posting / registration / filters.</summary>
public class HomepageDepartment
{
    public Guid DepartmentId { get; set; }

    public string Name { get; set; } = default!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Trade category master list (Construction, Welding, HVAC, ...).</summary>
public class HomepageTradeCategory
{
    public Guid TradeCategoryId { get; set; }

    public string Name { get; set; } = default!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// A "this isn't in your list" suggestion submitted by a candidate/recruiter
/// (e.g. from the registration industry dropdown) for one of the homepage
/// master lists above. Reviewed from the admin Suggestions tab.
/// </summary>
public class HomepageSuggestion
{
    public Guid SuggestionId { get; set; }

    /// <summary>Which list this suggestion targets.</summary>
    public HomepageSuggestionType Type { get; set; }

    public string SuggestedName { get; set; } = default!;

    public string? Note { get; set; }

    public Guid? SubmittedByUserId { get; set; }

    public string? SubmittedByName { get; set; }

    public string? SubmittedByEmail { get; set; }

    public HomepageSuggestionStatus Status { get; set; } = HomepageSuggestionStatus.Pending;

    public string? AdminNote { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? SubmittedByUser { get; set; }

    public AdminUser? ReviewedByAdmin { get; set; }
}

public enum HomepageSuggestionType
{
    Industry,
    Location,
    Role,
    RegistrationIndustry,
    Department,
    TradeCategory
}

public enum HomepageSuggestionStatus
{
    Pending,
    Approved,
    Rejected
}