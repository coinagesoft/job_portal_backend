using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Public;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Candidate;

public class PublicCompanyService : IPublicCompanyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PublicCompanyService> _logger;

    public PublicCompanyService(
        AppDbContext context,
        ILogger<PublicCompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PublicCompanyListResponseDto> GetCompaniesAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            //-------------------------------------------------------
            // Load Companies
            //-------------------------------------------------------

            var companies = await _context.EmployerProfiles
                .AsNoTracking()
                .Include(x => x.Badges)
                .OrderBy(x => x.CompanyDisplayName)
                .ToListAsync();

            //-------------------------------------------------------
            // Active Jobs Count
            //-------------------------------------------------------

            var activeJobs = await _context.JobPostings
                .AsNoTracking()
                .Where(x =>
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today)
                .GroupBy(x => x.EmployerId)
                .Select(x => new
                {
                    EmployerId = x.Key,
                    Count = x.Count()
                })
                .ToDictionaryAsync(
                    x => x.EmployerId,
                    x => x.Count);

            //-------------------------------------------------------
            // Cards
            //-------------------------------------------------------

            var cards = companies
                .Select(company =>
                {
                    activeJobs.TryGetValue(
                        company.EmployerId,
                        out int openJobs);

                    return new PublicCompanyCardDto
                    {
                        EmployerId =
                            company.EmployerId,

                        CompanyName =
                            company.CompanyDisplayName,

                        CompanyLogoUrl =
                            company.CompanyLogoUrl,

                        CoverImageUrl =
                            company.CoverImageUrl,

                        Industry =
                            company.IndustryType.ToString(),

                        City =
                            company.City,

                        State =
                            company.State,

                        IsVerified =
                            company.Badges.Any(x =>
                                x.BadgeStatus ==
                                BadgeStatus.Approved),

                        OpenJobsCount =
                            openJobs
                    };
                })
                .OrderByDescending(x => x.OpenJobsCount)
                .ThenBy(x => x.CompanyName)
                .ToList();

            //-------------------------------------------------------
            // Response
            //-------------------------------------------------------

            return new PublicCompanyListResponseDto
            {
                Success = true,

                Message =
                    $"{cards.Count} companies found.",

                Companies = cards
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PublicCompanyService.GetCompaniesAsync failed.");

            return new PublicCompanyListResponseDto
            {
                Success = false,

                Message =
                    ex.InnerException?.Message ??
                    ex.Message
            };
        }
    }

    //==========================================================
    // PART 2
    //==========================================================

    public async Task<PublicCompanyDetailResponseDto> GetCompanyDetailAsync(Guid employerId)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            //-------------------------------------------------------
            // Company
            //-------------------------------------------------------

            var company = await _context.EmployerProfiles
                .AsNoTracking()
                .Include(x => x.Badges)
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (company == null)
            {
                return new PublicCompanyDetailResponseDto
                {
                    Success = false,
                    Message = "Company not found."
                };
            }

            //-------------------------------------------------------
            // Open Jobs
            //-------------------------------------------------------

            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Include(x => x.EmployerProfile)
                    .ThenInclude(x => x.Badges)
                .Where(x =>
                    x.EmployerId == employerId &&
                    x.JobStatus == JobStatus.Active &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.ApplicationDeadline >= today)
                .OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.PublishedAt)
                .ToListAsync();

            //-------------------------------------------------------
            // Response
            //-------------------------------------------------------

            return new PublicCompanyDetailResponseDto
            {
                Success = true,

                Message = "Company details retrieved successfully.",

                EmployerId = company.EmployerId,

                CompanyName = company.CompanyDisplayName,

                CompanyLogoUrl = company.CompanyLogoUrl,

                CoverImageUrl = company.CoverImageUrl,

                CompanyDescription = company.CompanyDescription,

                Industry = company.IndustryType.ToString(),

                BusinessType = company.BusinessType.ToString(),

                CompanySize = company.CompanySize?.ToString(),

                YearEstablished = company.YearEstablished,

                WebsiteUrl = company.WebsiteUrl,

                LinkedInUrl = company.LinkedInUrl,

                InstagramUrl = company.InstagramUrl,

                FacebookUrl = company.FacebookUrl,

                AddressLine1 = company.AddressLine1,

                AddressLine2 = company.AddressLine2,

                City = company.City,

                State = company.State,

                Country = company.Country,

                Pincode = company.Pincode,

                GstRegistered = company.GstRegistered,

                HasPoeLicence =
                    !string.IsNullOrWhiteSpace(company.PoeLicenceUrl),

                HasRpslLicence =
                    !string.IsNullOrWhiteSpace(company.RpslLicenceUrl),

                IsVerified =
                    company.Badges.Any(x =>
                        x.BadgeStatus == BadgeStatus.Approved),

                CompanyHighlights =
                    company.CompanyHighlights ?? new List<string>(),

                TotalEmployees =
                    company.TotalEmployees,

                OpenJobsCount =
                    jobs.Count,

                Jobs =
                    jobs.Select(MapToCard)
                        .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PublicCompanyService.GetCompanyDetailAsync failed. EmployerId={EmployerId}",
                employerId);

            return new PublicCompanyDetailResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    private CandidateJobListItemDto MapToCard(JobPosting job)
    {
        return new CandidateJobListItemDto
        {
            JobId = job.JobId,

            EmployerId = job.EmployerId,

            CompanyLogoUrl =
                job.CompanyVisibility == CompanyVisibility.ShowName
                    ? job.EmployerProfile?.CompanyLogoUrl
                    : null,

            CompanyName =
                job.CompanyVisibility == CompanyVisibility.ShowName
                    ? job.EmployerProfile?.CompanyDisplayName
                    : "Confidential Company",

            JobTitle = job.JobTitle,

            TradeCategory = job.TradeCategory,

            Department = job.Department,

            EmploymentType =
                job.EmploymentType.ToString(),

            EmploymentMode =
                job.EmploymentMode.ToString(),

            JobType =
                job.JobType.ToString(),

            JobLocation =
                GetJobLocation(job),

            CompanyLocation =
                GetCompanyLocation(job),

            SalaryDisplay =
                FormatSalary(job),

            ExperienceDisplay =
                GetExperienceDisplay(job),

            Vacancies =
                job.Vacancies,

            ApplicationsCount =
                job.AppliedCount,

            ViewCount =
                job.ViewCount,

            PostedOn =
                job.PublishedAt,

            TimeAgo =
                GetTimeAgo(job.PublishedAt),

            Description =
                TruncateDescription(job.JobDescription, 180),

            Skills =
                job.KeySkills?
                    .Take(5)
                    .ToList()
                ?? new List<string>(),

            IsFeatured =
                job.IsFeatured,

            IsUrgentHiring =
                job.IsUrgentHiring,

            PassportRequired =
                job.PassportRequired,

            IsInternational =
                job.IsInternational,

            CompanyVerified =
                IsVerified(job.EmployerProfile),

            ApplicationDeadline =
                job.ApplicationDeadline
        };
    }


    private static string FormatSalary(JobPosting job)
    {
        if (job.SalaryDisplayOption ==
            SalaryDisplayOption.Negotiable)
        {
            return "Negotiable";
        }

        string currency = job.SalaryCurrency.ToString();

        return job.SalaryDisplayOption switch
        {
            SalaryDisplayOption.Show_Min_Only =>
                $"{currency} {job.SalaryMin:N0}+",

            SalaryDisplayOption.Show_Max_Only =>
                $"Up to {currency} {job.SalaryMax:N0}",

            _ =>
                $"{currency} {job.SalaryMin:N0} - {job.SalaryMax:N0}"
        };
    }

    private static string GetExperienceDisplay(JobPosting job)
    {
        if (job.ExperienceMinYears == 0 &&
            job.ExperienceMaxYears == 0)
        {
            return "Fresher";
        }

        if (job.ExperienceMaxYears == 0)
        {
            return $"{job.ExperienceMinYears}+ Years";
        }

        return $"{job.ExperienceMinYears}-{job.ExperienceMaxYears} Years";
    }

    private static string GetJobLocation(JobPosting job)
    {
        if (job.LocationType == LocationType.Offshore)
        {
            return string.Join(", ",
                new[]
                {
                job.OffshoreRegion,
                job.OffshoreCountry
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        return string.Join(", ",
            new[]
            {
            job.OnshoreCity,
            job.OnshoreState
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
    private static string GetCompanyLocation(JobPosting job)
    {
        return string.Join(", ",
            new[]
            {
            job.EmployerProfile?.City,
            job.EmployerProfile?.State
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
    private static bool IsVerified(
    EmployerProfile? employer)
    {
        if (employer == null)
            return false;

        return employer.Badges.Any(x =>
            x.BadgeStatus == BadgeStatus.Approved);
    }

    private static string TruncateDescription(
    string? description,
    int maxLength)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        if (description.Length <= maxLength)
            return description;

        return description.Substring(0, maxLength) + "...";
    }

    private static string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "Recently";

        var span = DateTime.UtcNow - dateTime.Value;

        if (span.TotalMinutes < 1)
            return "Just now";

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} min ago";

        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hr ago";

        if (span.TotalDays < 30)
            return $"{(int)span.TotalDays} day(s) ago";

        if (span.TotalDays < 365)
            return $"{(int)(span.TotalDays / 30)} month(s) ago";

        return $"{(int)(span.TotalDays / 365)} year(s) ago";
    }


}


