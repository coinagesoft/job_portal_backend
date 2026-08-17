// ============================================================
//  JobPortal.Application/DTOs/Public/HomepageDtos.cs
// ============================================================

namespace JobPortal.Application.DTOs.Public;

public class HomepageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CategoryCardDto> BrowseByCategory { get; set; } = new();
    public LatestJobsSectionDto LatestJobs { get; set; } = new();
    public JobsOfTheDaySectionDto JobsOfTheDay { get; set; } = new();
    public List<JobsByRoleCardDto> JobsByRole { get; set; } = new();
    public List<string> PopularSearchKeywords { get; set; } = new();
}

public class CategoryCardDto
{
    public string TradeCategory { get; set; } = default!;
    public int JobCount { get; set; }
    public string? IconSlug { get; set; }
}

public class LatestJobsSectionDto
{
    public List<string> CountryTabs { get; set; } = new();
    public Dictionary<string, List<HomepageJobCardDto>> JobsByCountry { get; set; } = new();
}

public class HomepageJobCardDto
{
    public Guid JobId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }
    public string JobTitle { get; set; } = default!;
    public string TradeCategory { get; set; } = default!;
    public string? EmploymentType { get; set; } = default!;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public bool IsInternational { get; set; }
    public string? SalaryDisplay { get; set; }
    public string SalaryCurrency { get; set; } = "INR";
    public List<string> Tags { get; set; } = new();
    public List<string> KeySkills { get; set; } = new();
    public string TimeAgo { get; set; } = default!;
    public DateTime? PublishedAt { get; set; }
    public bool IsUrgent { get; set; }
    public string? CoverImageUrl { get; set; }

    public string? JobType { get; set; }
    public string? EmploymentMode { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsUrgentHiring { get; set; }
}

public class JobsOfTheDaySectionDto
{
    public List<string> CategoryTabs { get; set; } = new();
    public Dictionary<string, List<HomepageJobCardDto>> JobsByCategory { get; set; } = new();
}

public class JobsByRoleCardDto
{
    public string RoleGroup { get; set; } = default!;
    public int JobCount { get; set; }
    public string? BackgroundImageUrl { get; set; }
}

public class HomepageRequestDto
{
    /// <summary>Defaults to "India". One of: India | UAE | Saudi Arabia | Qatar | Singapore</summary>
    public string DefaultCountry { get; set; } = "India";
    public string? DefaultCategory { get; set; }
}

// ── "Suggest a category/location/role we're missing" ──────────────
// Lets a candidate/recruiter suggest a value that isn't in one of the
// admin-managed homepage lists. Feeds the admin Suggestions tab.

public class SubmitSuggestionRequestDto
{
    /// <summary>Industry | Location | Role | RegistrationIndustry | Department | TradeCategory</summary>
    public string Type { get; set; } = default!;
    public string SuggestedName { get; set; } = default!;
    public string? Note { get; set; }
    public string? SubmittedByName { get; set; }
    public string? SubmittedByEmail { get; set; }
}

public class SubmitSuggestionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public Guid? SuggestionId { get; set; }
}

// ── GET api/public/homepage/data ───────────────────────────────────
// Everything managed from the Admin "Homepage Management" screen
// (Hero / Industries / Statistics / Locations / Roles /
// Registration Industries / Departments / Trade Categories), read-only,
// active items only, in display order. This is separate from
// HomepageResponseDto above, which carries live job-listing sections.

public class PublicHomepageContentResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;

    public PublicHeroDto Hero { get; set; } = new();
    public List<PublicIndustryDto> Industries { get; set; } = new();
    public List<PublicStatItemDto> Statistics { get; set; } = new();
    public List<PublicLocationDto> Locations { get; set; } = new();
    public List<PublicRoleDto> Roles { get; set; } = new();
    public List<PublicNamedListItemDto> RegistrationIndustries { get; set; } = new();
    public List<PublicNamedListItemDto> Departments { get; set; } = new();
    public List<PublicNamedListItemDto> TradeCategories { get; set; } = new();
}

public class PublicHeroDto
{
    public string Headline { get; set; } = default!;
    public string? Subheadline { get; set; }
    public string? SearchPlaceholder { get; set; }
    public string? CtaText { get; set; }
    public string? CtaLink { get; set; }
    public string? BannerImageUrl { get; set; }
}

public class PublicIndustryDto
{
    public Guid IndustryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public int JobCount { get; set; }
    public int DisplayOrder { get; set; }
}

public class PublicStatItemDto
{
    public string Label { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? Suffix { get; set; }
    public string? IconSlug { get; set; }
    public int DisplayOrder { get; set; }
}

public class PublicLocationDto
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = default!;
    public string? Country { get; set; }
    public string? ImageUrl { get; set; }
    public int JobCount { get; set; }
    public int DisplayOrder { get; set; }
}

public class PublicRoleDto
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public int JobCount { get; set; }
    public int DisplayOrder { get; set; }
}

public class PublicNamedListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
}