// ============================================================
//  JobPortal.Application/DTOs/Admin/Homepage/HomepageManagementDtos.cs
//
//  Request/response shapes for the Admin "Homepage Management" screen.
//  Kept in one file since every section follows the same small shape
//  (list item + create + update + a couple of DTO-less toggles).
// ============================================================

using JobPortal.Domain.Entities.Homepage;
using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Homepage
{
    // ── Shared ──────────────────────────────────────────────────────

    /// <summary>Generic ok/message envelope for actions that don't need to return a record.</summary>
    public class HomepageActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
    }

    // ── Hero Section ────────────────────────────────────────────────

    public class HeroSectionDto
    {
        public Guid HeroId { get; set; }
        public string Headline { get; set; } = default!;
        public string? Subheadline { get; set; }
        public string? SearchPlaceholder { get; set; }
        public string? CtaText { get; set; }
        public string? CtaLink { get; set; }
        public string? BannerImageUrl { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateHeroSectionRequestDto
    {
        public string Headline { get; set; } = default!;
        public string? Subheadline { get; set; }
        public string? SearchPlaceholder { get; set; }
        public string? CtaText { get; set; }
        public string? CtaLink { get; set; }
    }

    // ── Browse by Industry ──────────────────────────────────────────

    public class IndustryDto
    {
        public Guid IndustryId { get; set; }
        public string Name { get; set; } = default!;
        public string? Slug { get; set; }
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool ShowInDropdown { get; set; }
    }

    public class CreateIndustryRequestDto
    {
        public string Name { get; set; } = default!;
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public bool ShowInDropdown { get; set; } = true;
    }

    public class UpdateIndustryRequestDto
    {
        public string? Name { get; set; }
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public int? DisplayOrder { get; set; }
    }

    // ── Hiring Statistics ───────────────────────────────────────────

    public class StatisticsDto
    {
        public List<HomepageStatItem> Items { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateStatisticsRequestDto
    {
        public List<HomepageStatItem> Items { get; set; } = new();
    }

    // ── Browse Jobs by Location ─────────────────────────────────────

    public class LocationDto
    {
        public Guid LocationId { get; set; }
        public string Name { get; set; } = default!;
        public string? Country { get; set; }
        public string? ImageUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateLocationRequestDto
    {
        public string Name { get; set; } = default!;
        public string? Country { get; set; }
        public int? JobCountOverride { get; set; }
    }

    public class UpdateLocationRequestDto
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public int? JobCountOverride { get; set; }
        public int? DisplayOrder { get; set; }
    }

    // ── Browse Jobs by Role ─────────────────────────────────────────

    public class RoleDto
    {
        public Guid RoleId { get; set; }
        public string Name { get; set; } = default!;
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateRoleRequestDto
    {
        public string Name { get; set; } = default!;
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
    }

    public class UpdateRoleRequestDto
    {
        public string? Name { get; set; }
        public string? IconUrl { get; set; }
        public int? JobCountOverride { get; set; }
        public int? DisplayOrder { get; set; }
    }

    // ── Registration Industries / Departments / Trade Categories ────
    // These three sections are structurally identical (Id, Name,
    // DisplayOrder, IsActive), so they share one DTO shape.

    public class NamedListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateNamedListItemRequestDto
    {
        public string Name { get; set; } = default!;
    }

    public class UpdateNamedListItemRequestDto
    {
        public string? Name { get; set; }
        public int? DisplayOrder { get; set; }
    }

    // ── Suggestions ─────────────────────────────────────────────────

    public class SuggestionDto
    {
        public Guid SuggestionId { get; set; }
        public HomepageSuggestionType Type { get; set; }
        public string SuggestedName { get; set; } = default!;
        public string? Note { get; set; }
        public string? SubmittedByName { get; set; }
        public string? SubmittedByEmail { get; set; }
        public HomepageSuggestionStatus Status { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ReviewSuggestionRequestDto
    {
        public string? AdminNote { get; set; }

        /// <summary>
        /// Approve only: when true, the suggested name is inserted into the
        /// target list (Industry/Location/Role/...) as a new active item.
        /// Defaults to true — set false to approve without auto-creating.
        /// </summary>
        public bool AddToList { get; set; } = true;
    }

}