// ============================================================
//  JobPortal.Services/Implement/Public/HomepageService.cs
// ============================================================

using JobPortal.Application.DTOs.Public;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobPortal.Services.Implement.Public;

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

    public async Task<HomepageResponseDto> GetHomepageDataAsync(HomepageRequestDto request)
    {
        try
        {
            var activeJobs = await _context.JobPostings
                .Include(j => j.EmployerProfile)
                .Where(j => j.JobStatus.ToString() == "Active")
                .Select(j => new
                {
                    j.JobId,
                    j.JobTitle,
                    j.TradeCategory,
                    j.PublishingTags,
                    j.KeySkills,
                    j.OnshoreCity,
                    j.OnshoreState,
                    j.OffshoreRegion,
                    j.IsInternational,
                    j.SalaryMin,
                    j.SalaryMax,
                    j.SalaryCurrency,
                    j.SalaryDisplayOption,
                    j.PublishedAt,
                    j.CompanyVisibility,
                    CompanyName = j.EmployerProfile != null ? j.EmployerProfile.CompanyDisplayName : null,
                    CompanyLogoUrl = j.EmployerProfile != null ? j.EmployerProfile.CompanyLogoUrl : null,
                    Country = j.IsInternational ? j.OffshoreRegion : "India"
                })
                .ToListAsync();

            // 1. Browse by Category
            var browseByCategory = activeJobs
                .GroupBy(j => j.TradeCategory ?? "Other")
                .Select(g => new CategoryCardDto
                {
                    TradeCategory = g.Key,
                    JobCount = g.Count(),
                    IconSlug = g.Key.ToLower().Replace(" ", "-").Replace("&", "and")
                })
                .OrderByDescending(c => c.JobCount)
                .ToList();

            // 2. Latest Jobs by Country
            var latestJobsByCountry = new Dictionary<string, List<HomepageJobCardDto>>();
            foreach (var country in CountryTabs)
            {
                var aliases = CountryAliases[country];
                latestJobsByCountry[country] = activeJobs
                    .Where(j => aliases.Any(a =>
                        string.Equals(j.Country, a, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(j => j.PublishedAt)
                    .Take(6)
                    .Select(j => MapToCard(j.JobId, j.JobTitle, j.TradeCategory,
                        j.PublishingTags, j.KeySkills, j.CompanyName, j.CompanyLogoUrl,
                        j.CompanyVisibility, j.OnshoreCity, j.OnshoreState, j.Country,
                        j.IsInternational, j.SalaryMin, j.SalaryMax, j.SalaryCurrency,
                        j.SalaryDisplayOption, j.PublishedAt))
                    .ToList();
            }

            // 3. Jobs of the Day by Category
            var jobsOfTheDayByCategory = new Dictionary<string, List<HomepageJobCardDto>>();
            foreach (var category in JobsOfTheDayCategories)
            {
                jobsOfTheDayByCategory[category] = activeJobs
                    .Where(j => string.Equals(j.TradeCategory, category, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(j => j.PublishedAt)
                    .Take(8)
                    .Select(j => MapToCard(j.JobId, j.JobTitle, j.TradeCategory,
                        j.PublishingTags, j.KeySkills, j.CompanyName, j.CompanyLogoUrl,
                        j.CompanyVisibility, j.OnshoreCity, j.OnshoreState, j.Country,
                        j.IsInternational, j.SalaryMin, j.SalaryMax, j.SalaryCurrency,
                        j.SalaryDisplayOption, j.PublishedAt))
                    .ToList();
            }

            // 4. Jobs by Role
            var roleGroups = activeJobs
                .Where(j => RoleGroupMap.ContainsKey(j.TradeCategory ?? ""))
                .GroupBy(j => RoleGroupMap[j.TradeCategory!])
                .Select(g => new JobsByRoleCardDto
                {
                    RoleGroup = g.Key,
                    JobCount = g.Count(),
                    BackgroundImageUrl = null
                })
                .OrderByDescending(r => r.JobCount)
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
                PopularSearchKeywords = PopularKeywords.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HomepageService.GetHomepageDataAsync failed.");
            return new HomepageResponseDto { Success = false, Message = "An error occurred while loading the homepage." };
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
}