// ============================================================
//  JobPortal.Services/Implement/Public/HomepageService.cs
// ============================================================

using JobPortal.Application.DTOs.Public;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobPortal.Services.Implement.Candidate;

public class HomepageService : IHomepageService
{
    private readonly AppDbContext _context;
    private readonly ILogger<HomepageService> _logger;

    private static readonly string[] CountryTabs =
        { "India", "UAE", "Saudi Arabia", "Qatar", "Singapore" };

    private static readonly string[] JobsOfTheDayCategories =
        { "Construction", "Technician", "Oil & Gas", "Factory", "Logistics", "Mechanical" };

    private static readonly Dictionary<string, string> RoleGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Oil & Gas"] = "Oil and Gas",
        ["Factory Worker"] = "Manufacturing Roles",
        ["Welder"] = "Welding and Fabrication",
        ["Fabricator"] = "Welding and Fabrication",
        ["Electrical"] = "Electrical and HVAC",
        ["HVAC"] = "Electrical and HVAC",
        ["Construction"] = "Construction and Civil",
        ["Logistics"] = "Logistics and Transport",
        ["Driver"] = "Logistics and Transport",
        ["Mechanical"] = "Mechanical and Maintenance",
        ["Technician"] = "Technical and Skilled Trades",
        ["Marine"] = "Marine and Offshore",
        ["Safety Officer"] = "Safety and Compliance",
        ["Site Supervisor"] = "Site Management",
    };

    private static readonly string[] PopularKeywords =
        { "Welder", "HVAC", "Driver", "Electrician", "Pipe Fitter", "Construction", "Marine" };

    private static readonly Dictionary<string, string[]> CountryAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["India"] = new[] { "India", "IN" },
            ["UAE"] = new[] { "UAE", "United Arab Emirates", "AE" },
            ["Saudi Arabia"] = new[] { "Saudi Arabia", "Saudi", "SA" },
            ["Qatar"] = new[] { "Qatar", "QA" },
            ["Singapore"] = new[] { "Singapore", "SG" },
        };

    public HomepageService(AppDbContext context, ILogger<HomepageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HomepageResponseDto> GetHomepageDataAsync(
       HomepageRequestDto request)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeJobs = await _context.JobPostings
                .AsNoTracking()
                .Include(j => j.EmployerProfile)
                .Where(j =>
                    j.JobStatus == JobStatus.Active &&
                    j.IsActive &&
                    !j.IsDeleted &&
                    j.ApplicationDeadline >= today)
                .Select(j => new
                {
                    j.JobId,
                    j.JobTitle,
                    j.TradeCategory,
                    j.Tags,
                    j.KeySkills,

                    j.OnshoreCity,
                    j.OnshoreState,
                    j.OnshoreCountry,

                    j.OffshoreRegion,
                    j.OffshoreCountry,

                    j.IsInternational,

                    j.SalaryMin,
                    j.SalaryMax,
                    j.SalaryCurrency,
                    j.SalaryDisplayOption,

                    j.JobType,
                    j.EmploymentType,
                    j.EmploymentMode,

                    j.IsFeatured,
                    j.IsUrgentHiring,

                    j.PublishedAt,
                    j.CompanyVisibility,

                    CompanyName = j.EmployerProfile != null
                        ? j.EmployerProfile.CompanyDisplayName
                        : null,

                    CompanyLogoUrl = j.EmployerProfile != null
                        ? j.EmployerProfile.CompanyLogoUrl
                        : null,

                    Country = j.IsInternational
                        ? (j.OffshoreCountry ?? "International")
                        : (j.OnshoreCountry ?? "India")
                })
                .ToListAsync();

            // =====================================================
            // Browse By Category
            // =====================================================

            var browseByCategory = activeJobs
                .GroupBy(j => string.IsNullOrWhiteSpace(j.TradeCategory)
                    ? "Other"
                    : j.TradeCategory)
                .Select(g => new CategoryCardDto
                {
                    TradeCategory = g.Key,
                    JobCount = g.Count(),
                    IconSlug = g.Key
                        .ToLower()
                        .Replace(" ", "-")
                        .Replace("&", "and")
                })
                .OrderByDescending(x => x.JobCount)
                .ToList();

            // =====================================================
            // Latest Jobs
            // =====================================================

            var latestJobsByCountry =
                new Dictionary<string, List<HomepageJobCardDto>>();

            foreach (var country in CountryTabs)
            {
                var aliases = CountryAliases[country];

                latestJobsByCountry[country] = activeJobs
                    .Where(j =>
                        aliases.Any(a =>
                            string.Equals(
                                j.Country,
                                a,
                                StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(j => j.PublishedAt)
                    .Take(6)
                    .Select(j => new HomepageJobCardDto
                    {
                        JobId = j.JobId,
                        JobTitle = j.JobTitle,
                        TradeCategory = j.TradeCategory,

                        CompanyName =
                            j.CompanyVisibility == CompanyVisibility.ShowName
                                ? j.CompanyName
                                : null,

                        CompanyLogoUrl =
                            j.CompanyVisibility == CompanyVisibility.ShowName
                                ? j.CompanyLogoUrl
                                : null,

                        City = j.OnshoreCity,
                        State = j.OnshoreState,
                        Country = j.Country,

                        IsInternational = j.IsInternational,

                        SalaryDisplay = BuildSalaryDisplay(
                            j.SalaryMin,
                            j.SalaryMax,
                            j.SalaryCurrency,
                            j.SalaryDisplayOption),

                        PublishedAt = j.PublishedAt,
                        TimeAgo = GetTimeAgo(j.PublishedAt),

                        KeySkills = j.KeySkills ?? new List<string>(),
                        Tags = j.Tags ?? new List<string>(),

                        JobType = j.JobType.ToString(),
                        EmploymentType = j.EmploymentType.ToString(),
                        EmploymentMode = j.EmploymentMode.ToString(),

                        IsFeatured = j.IsFeatured,
                        IsUrgentHiring = j.IsUrgentHiring
                    })
                    .ToList();
            }

            // =====================================================
            // Jobs Of The Day
            // =====================================================

            var jobsOfTheDayByCategory =
                new Dictionary<string, List<HomepageJobCardDto>>();

            foreach (var category in JobsOfTheDayCategories)
            {
                jobsOfTheDayByCategory[category] = activeJobs
                    .Where(j =>
                        string.Equals(
                            j.TradeCategory,
                            category,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(j => j.PublishedAt)
                    .Take(8)
                    .Select(j => new HomepageJobCardDto
                    {
                        JobId = j.JobId,
                        JobTitle = j.JobTitle,
                        TradeCategory = j.TradeCategory,

                        CompanyName =
                            j.CompanyVisibility == CompanyVisibility.ShowName
                                ? j.CompanyName
                                : null,

                        CompanyLogoUrl =
                            j.CompanyVisibility == CompanyVisibility.ShowName
                                ? j.CompanyLogoUrl
                                : null,

                        City = j.OnshoreCity,
                        State = j.OnshoreState,
                        Country = j.Country,

                        IsInternational = j.IsInternational,

                        SalaryDisplay = BuildSalaryDisplay(
                            j.SalaryMin,
                            j.SalaryMax,
                            j.SalaryCurrency,
                            j.SalaryDisplayOption),

                        PublishedAt = j.PublishedAt,
                        TimeAgo = GetTimeAgo(j.PublishedAt),

                        KeySkills = j.KeySkills ?? new List<string>(),
                        Tags = j.Tags ?? new List<string>(),

                        JobType = j.JobType.ToString(),
                        EmploymentType = j.EmploymentType.ToString(),
                        EmploymentMode = j.EmploymentMode.ToString(),

                        IsFeatured = j.IsFeatured,
                        IsUrgentHiring = j.IsUrgentHiring
                    })
                    .ToList();
            }

            // =====================================================
            // Jobs By Role
            // =====================================================

            var roleGroups = activeJobs
                .Where(j =>
                    !string.IsNullOrWhiteSpace(j.TradeCategory) &&
                    RoleGroupMap.ContainsKey(j.TradeCategory))
                .GroupBy(j => RoleGroupMap[j.TradeCategory])
                .Select(g => new JobsByRoleCardDto
                {
                    RoleGroup = g.Key,
                    JobCount = g.Count(),
                    BackgroundImageUrl = null
                })
                .OrderByDescending(x => x.JobCount)
                .ToList();

            return new HomepageResponseDto
            {
                Success = true,
                Message = "Homepage data loaded successfully.",

                BrowseByCategory = browseByCategory,

                LatestJobs = new LatestJobsSectionDto
                {
                    CountryTabs = CountryTabs.ToList(),
                    JobsByCountry = latestJobsByCountry
                },

                JobsOfTheDay = new JobsOfTheDaySectionDto
                {
                    CategoryTabs = JobsOfTheDayCategories.ToList(),
                    JobsByCategory = jobsOfTheDayByCategory
                },

                JobsByRole = roleGroups,

                PopularSearchKeywords =
                    PopularKeywords.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "HomepageService.GetHomepageDataAsync failed.");

            return new HomepageResponseDto
            {
                Success = false,
                Message = "An error occurred while loading the homepage."
            };
        }
    }
    private static HomepageJobCardDto MapToCard(
        Guid jobId, string jobTitle, string? tradeCategory,
        string? publishingTagsJson, string? keySkillsJson,
        string? companyName, string? companyLogoUrl, string? companyVisibility,
        string? city, string? state, string? country, bool isInternational,
        int? salaryMin, int? salaryMax, string? salaryCurrency, string? salaryDisplayOption,
        DateTime? publishedAt)
    {
        var isConfidential = companyVisibility == "Confidential_Client";
        var tags = ParseJsonList(publishingTagsJson);
        var skills = ParseJsonList(keySkillsJson).Take(3).ToList();
        var known = new HashSet<string> { "Permanent", "Contract", "Temporary", "Internship" };

        string? salaryDisplay = null;
        if (salaryDisplayOption != "Confidential")
        {
            var symbol = (salaryCurrency ?? "INR") switch { "USD" => "$", "AED" => "AED ", "SAR" => "SAR ", _ => "₹" };
            salaryDisplay = salaryDisplayOption == "Show_Min_Only"
                ? $"{symbol}{salaryMin:N0}+"
                : $"{symbol}{salaryMin:N0} – {symbol}{salaryMax:N0} / month";
        }

        DateTime? pub = publishedAt;
        string timeAgo = "Recently";
        if (pub != null)
        {
            var diff = DateTime.UtcNow - pub.Value;
            if (diff.TotalMinutes < 60) timeAgo = $"{(int)diff.TotalMinutes} mins ago";
            else if (diff.TotalHours < 24) timeAgo = $"{(int)diff.TotalHours} hours ago";
            else if (diff.TotalDays < 7) timeAgo = $"{(int)diff.TotalDays} days ago";
            else if (diff.TotalDays < 30) timeAgo = $"{(int)(diff.TotalDays / 7)} weeks ago";
            else timeAgo = $"{(int)(diff.TotalDays / 30)} months ago";
        }

        return new HomepageJobCardDto
        {
            JobId = jobId,
            CompanyName = isConfidential ? null : companyName,
            CompanyLogoUrl = isConfidential ? null : companyLogoUrl,
            IsConfidentialCompany = isConfidential,
            JobTitle = jobTitle,
            TradeCategory = tradeCategory ?? "Other",
            EmploymentType = tags.FirstOrDefault(t => known.Contains(t)) ?? "Full time",
            City = city,
            State = state,
            Country = country,
            IsInternational = isInternational,
            SalaryDisplay = salaryDisplay,
            SalaryCurrency = salaryCurrency ?? "INR",
            Tags = tags,
            KeySkills = skills,
            TimeAgo = timeAgo,
            PublishedAt = publishedAt,
            IsUrgent = tags.Contains("Urgent_Hiring") || tags.Contains("Hot_Vacancy"),
            CoverImageUrl = null
        };
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
    private static string BuildSalaryDisplay(
    int min,
    int max,
    SalaryCurrency currency,
    SalaryDisplayOption displayOption)
    {
        if (displayOption == SalaryDisplayOption.Show_Range)
            return "Confidential";

        var symbol = currency switch
        {
            SalaryCurrency.USD => "$",
            SalaryCurrency.AED => "AED ",
            SalaryCurrency.SAR => "SAR ",
            _ => "₹"
        };

        return displayOption switch
        {
            SalaryDisplayOption.Show_Min_Only
                => $"{symbol}{min:N0}+",

            SalaryDisplayOption.Show_Max_Only
                => $"{symbol}{max:N0}",

            _
                => $"{symbol}{min:N0} - {symbol}{max:N0}"
        };
    }

    private static string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "Recently";

        var ts = DateTime.UtcNow - dateTime.Value;

        if (ts.TotalSeconds < 60)
            return $"{(int)ts.TotalSeconds} sec ago";

        if (ts.TotalMinutes < 60)
            return $"{(int)ts.TotalMinutes} min ago";

        if (ts.TotalHours < 24)
            return $"{(int)ts.TotalHours} hr ago";

        if (ts.TotalDays < 30)
            return $"{(int)ts.TotalDays} day(s) ago";

        if (ts.TotalDays < 365)
            return $"{(int)(ts.TotalDays / 30)} month(s) ago";

        return $"{(int)(ts.TotalDays / 365)} year(s) ago";
    }
}