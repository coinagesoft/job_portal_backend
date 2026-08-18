using JobPortal.Application.DTOs.Admin.Dashboard;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    // Powers Admin ▸ Dashboard only (https://.../admin/dashboard) — the
    // platform-wide summary page. Every method backs exactly one
    // section of the page so the frontend can call/refresh them
    // independently instead of loading one giant payload.
    public class AdminDashboardService : IAdminDashboardService
    {
        private const string CompletedStatus = "Completed";

        private readonly AppDbContext _db;

        public AdminDashboardService(AppDbContext db)
        {
            _db = db;
        }

        // ------------------------------------------------------------
        // 1. STATS WIDGETS
        // ------------------------------------------------------------
        public async Task<StatsWidgetsResponseDto> GetStatsWidgetsAsync()
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            // ---- Revenue (completed transactions) ----
            var txnRows = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.PaymentStatus == CompletedStatus)
                .Select(t => new { t.TotalAmountPaise, t.CreditQuantity, t.CreatedAt })
                .ToListAsync();

            var totalRevenue = txnRows.Sum(t => (decimal)t.TotalAmountPaise) / 100m;
            var currentMonthRevenue = txnRows
                .Where(t => t.CreatedAt >= currentMonthStart)
                .Sum(t => (decimal)t.TotalAmountPaise) / 100m;
            var previousMonthRevenue = txnRows
                .Where(t => t.CreatedAt >= previousMonthStart && t.CreatedAt < currentMonthStart)
                .Sum(t => (decimal)t.TotalAmountPaise) / 100m;

            var totalCreditsSold = txnRows.Sum(t => (long?)t.CreditQuantity) ?? 0;
            var currentMonthCredits = txnRows
                .Where(t => t.CreatedAt >= currentMonthStart)
                .Sum(t => (long?)t.CreditQuantity) ?? 0;
            var previousMonthCredits = txnRows
                .Where(t => t.CreatedAt >= previousMonthStart && t.CreatedAt < currentMonthStart)
                .Sum(t => (long?)t.CreditQuantity) ?? 0;

            // ---- Candidates / Recruiters (from Users, by UserType) ----
            var userRows = await _db.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted &&
                    (u.UserType == UserType.Candidate || u.UserType == UserType.Recruiter))
                .Select(u => new { u.UserType, u.CreatedAt })
                .ToListAsync();

            var totalCandidates = userRows.Count(u => u.UserType == UserType.Candidate);
            var currentMonthCandidates = userRows.Count(u => u.UserType == UserType.Candidate && u.CreatedAt >= currentMonthStart);
            var previousMonthCandidates = userRows.Count(u => u.UserType == UserType.Candidate && u.CreatedAt >= previousMonthStart && u.CreatedAt < currentMonthStart);

            var totalRecruiters = userRows.Count(u => u.UserType == UserType.Recruiter);
            var currentMonthRecruiters = userRows.Count(u => u.UserType == UserType.Recruiter && u.CreatedAt >= currentMonthStart);
            var previousMonthRecruiters = userRows.Count(u => u.UserType == UserType.Recruiter && u.CreatedAt >= previousMonthStart && u.CreatedAt < currentMonthStart);

            // ---- Job postings ----
            var activeJobPostings = await _db.JobPostings
                .AsNoTracking()
                .Where(j => !j.IsDeleted && j.JobStatus == JobStatus.Active)
                .CountAsync();
            var pausedJobPostings = await _db.JobPostings
                .AsNoTracking()
                .Where(j => !j.IsDeleted && j.JobStatus == JobStatus.Paused)
                .CountAsync();

            // ---- Pending verifications (employer documents) ----
            var pendingVerificationRows = await _db.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(d => !d.IsDeleted && d.Status == VerificationDocumentStatus.Pending)
                .Select(d => d.UploadedAt)
                .ToListAsync();
            var highPriorityCutoff = now.AddDays(-7);

            // ---- Support tickets ----
            var ticketStatuses = await _db.SupportTickets
                .AsNoTracking()
                .Select(t => t.Status)
                .ToListAsync();

            return new StatsWidgetsResponseDto
            {
                TotalRevenue = BuildStatCard(totalRevenue, currentMonthRevenue, previousMonthRevenue),
                TotalCandidates = BuildStatCard(totalCandidates, currentMonthCandidates, previousMonthCandidates),
                TotalRecruiters = BuildStatCard(totalRecruiters, currentMonthRecruiters, previousMonthRecruiters),
                CreditsSold = BuildStatCard(totalCreditsSold, currentMonthCredits, previousMonthCredits),
                ActiveJobPostings = new JobPostingsStatDto
                {
                    Active = activeJobPostings,
                    Paused = pausedJobPostings
                },
                PendingVerifications = new PendingVerificationsStatDto
                {
                    Total = pendingVerificationRows.Count,
                    HighPriority = pendingVerificationRows.Count(d => d < highPriorityCutoff)
                },
                OpenSupportTickets = new SupportTicketsStatDto
                {
                    Open = ticketStatuses.Count(s => s != "Resolved"),
                    Pending = ticketStatuses.Count(s => s == "Open")
                }
            };
        }

        // month-over-month card helper — works for both decimal (revenue)
        // and integer (counts) values since everything is decimal underneath.
        private static StatCardDto BuildStatCard(decimal totalValue, decimal currentMonthValue, decimal previousMonthValue)
        {
            decimal? changePercent = null;
            string? direction = null;

            if (previousMonthValue > 0)
            {
                changePercent = Math.Round((currentMonthValue - previousMonthValue) / previousMonthValue * 100, 1);
                direction = changePercent >= 0 ? "up" : "down";
            }
            else if (currentMonthValue > 0)
            {
                changePercent = 100m;
                direction = "up";
            }

            return new StatCardDto
            {
                Value = totalValue,
                ChangePercent = changePercent,
                ChangeDirection = direction
            };
        }

        // ------------------------------------------------------------
        // 2. REGISTRATION GROWTH
        // ------------------------------------------------------------
        // Filter removed for QA testing — always "week" (last 7 days).
        public async Task<RegistrationGrowthResponseDto> GetRegistrationGrowthAsync()
        {
            const string range = "week";

            var now = DateTime.UtcNow;

            if (range == "week")
            {
                var today = now.Date;
                var start = today.AddDays(-6);

                var rows = await _db.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted &&
                        u.CreatedAt >= start &&
                        (u.UserType == UserType.Candidate || u.UserType == UserType.Recruiter))
                    .Select(u => new { u.UserType, u.CreatedAt })
                    .ToListAsync();

                var labels = new List<string>();
                var candidates = new List<int>();
                var recruiters = new List<int>();

                for (var day = start; day <= today; day = day.AddDays(1))
                {
                    labels.Add(day.ToString("ddd"));
                    candidates.Add(rows.Count(r => r.UserType == UserType.Candidate && r.CreatedAt.Date == day));
                    recruiters.Add(rows.Count(r => r.UserType == UserType.Recruiter && r.CreatedAt.Date == day));
                }

                return new RegistrationGrowthResponseDto
                {
                    Range = "week",
                    Labels = labels,
                    Candidates = candidates,
                    Recruiters = recruiters
                };
            }

            if (range == "month")
            {
                var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var start = currentMonthStart.AddMonths(-11);

                var rows = await _db.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted &&
                        u.CreatedAt >= start &&
                        (u.UserType == UserType.Candidate || u.UserType == UserType.Recruiter))
                    .Select(u => new { u.UserType, u.CreatedAt })
                    .ToListAsync();

                var labels = new List<string>();
                var candidates = new List<int>();
                var recruiters = new List<int>();

                for (var month = start; month <= currentMonthStart; month = month.AddMonths(1))
                {
                    var monthEnd = month.AddMonths(1);
                    labels.Add(month.ToString("MMM"));
                    candidates.Add(rows.Count(r => r.UserType == UserType.Candidate && r.CreatedAt >= month && r.CreatedAt < monthEnd));
                    recruiters.Add(rows.Count(r => r.UserType == UserType.Recruiter && r.CreatedAt >= month && r.CreatedAt < monthEnd));
                }

                return new RegistrationGrowthResponseDto
                {
                    Range = "month",
                    Labels = labels,
                    Candidates = candidates,
                    Recruiters = recruiters
                };
            }

            // range == "year" — last 6 calendar years, oldest first.
            var startYear = now.Year - 5;
            var yearStart = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var yearRows = await _db.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted &&
                    u.CreatedAt >= yearStart &&
                    (u.UserType == UserType.Candidate || u.UserType == UserType.Recruiter))
                .Select(u => new { u.UserType, u.CreatedAt })
                .ToListAsync();

            var yearLabels = new List<string>();
            var yearCandidates = new List<int>();
            var yearRecruiters = new List<int>();

            for (var year = startYear; year <= now.Year; year++)
            {
                yearLabels.Add(year.ToString());
                yearCandidates.Add(yearRows.Count(r => r.UserType == UserType.Candidate && r.CreatedAt.Year == year));
                yearRecruiters.Add(yearRows.Count(r => r.UserType == UserType.Recruiter && r.CreatedAt.Year == year));
            }

            return new RegistrationGrowthResponseDto
            {
                Range = "year",
                Labels = yearLabels,
                Candidates = yearCandidates,
                Recruiters = yearRecruiters
            };
        }

        // ------------------------------------------------------------
        // 3. RECRUITERS BY INDUSTRY
        // ------------------------------------------------------------
        // Shows the top 5 industries by recruiter count: the 4 largest
        // named industries, plus a single "Other" slice that folds in
        // (a) any profile whose IndustryType is literally blank/"Other",
        // and (b) every industry outside the top 4. Before grouping,
        // industry names are normalized (trimmed, whitespace collapsed,
        // compared case-insensitively) so casing/spacing typos like
        // "Manufacturing" vs "manufacturing " don't get counted as two
        // separate industries. Note: this does NOT merge genuinely
        // different labels for the same real-world industry (e.g.
        // "Construction" vs "Construction & Infrastructure") — those are
        // different strings with different meaning, so collapsing them
        // automatically would risk hiding a real data-entry difference.
        // If those need to be treated as one industry, they should be
        // unified at the source (fix the stored IndustryType values, or
        // maintain an explicit alias map) rather than guessed here.
        public async Task<RecruitersByIndustryResponseDto> GetRecruitersByIndustryAsync()
        {
            var rows = await _db.EmployerProfiles
                .AsNoTracking()
                .Where(e => e.AccountStatus != AccountStatus.Deleted)
                .Select(e => e.IndustryType)
                .ToListAsync();

            var total = rows.Count;

            var normalized = rows
                .Select(raw =>
                {
                    var cleaned = string.IsNullOrWhiteSpace(raw)
                        ? null
                        : System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");

                    var isOther = cleaned == null || cleaned.Equals("Other", StringComparison.OrdinalIgnoreCase);

                    return new
                    {
                        // Key used purely for grouping — case-insensitive,
                        // whitespace-normalized.
                        Key = isOther ? "other" : cleaned!.ToLowerInvariant(),
                        // Display label shown to the user; the raw "Other"
                        // bucket always displays as "Other".
                        Display = isOther ? "Other" : cleaned!,
                        IsOther = isOther
                    };
                })
                .ToList();

            var grouped = normalized
                .GroupBy(x => x.Key)
                .Select(g => new
                {
                    IsOther = g.Key == "other",
                    // Use whichever exact casing occurs most often within
                    // the group as the display label.
                    Display = g.GroupBy(x => x.Display)
                        .OrderByDescending(dg => dg.Count())
                        .First().Key,
                    Count = g.Count()
                })
                .ToList();

            var explicitOther = grouped.FirstOrDefault(g => g.IsOther);
            var namedIndustries = grouped
                .Where(g => !g.IsOther)
                .OrderByDescending(g => g.Count)
                .ToList();

            const int topN = 4;
            var top = namedIndustries.Take(topN).ToList();
            var overflowCount = namedIndustries.Skip(topN).Sum(g => g.Count);
            var otherCount = (explicitOther?.Count ?? 0) + overflowCount;

            var slices = top
                .Select(g => new IndustrySliceDto
                {
                    Industry = g.Display,
                    Count = g.Count,
                    Percentage = total > 0 ? Math.Round(g.Count * 100m / total, 1) : 0
                })
                .ToList();

            if (otherCount > 0)
            {
                slices.Add(new IndustrySliceDto
                {
                    Industry = "Other",
                    Count = otherCount,
                    Percentage = total > 0 ? Math.Round(otherCount * 100m / total, 1) : 0
                });
            }

            return new RecruitersByIndustryResponseDto
            {
                TotalRecruiters = total,
                Slices = slices
            };
        }

        // ------------------------------------------------------------
        // 4. REVENUE & CREDIT GROWTH
        // ------------------------------------------------------------
        // Filter removed for QA testing — always the last 6 months.
        public async Task<RevenueCreditGrowthResponseDto> GetRevenueCreditGrowthAsync()
        {
            const int months = 6;

            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var start = currentMonthStart.AddMonths(-(months - 1));

            var rows = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.PaymentStatus == CompletedStatus && t.CreatedAt >= start)
                .Select(t => new
                {
                    t.TotalAmountPaise,
                    t.CreditQuantity,
                    t.TransactionType,
                    t.CandidateId,
                    t.EmployerId,
                    t.CreatedAt
                })
                .ToListAsync();

            var labels = new List<string>();
            var candidateSeries = new List<decimal>();
            var recruiterSeries = new List<decimal>();
            var creditSeries = new List<decimal>();

            for (var month = start; month <= currentMonthStart; month = month.AddMonths(1))
            {
                var monthEnd = month.AddMonths(1);
                var monthRows = rows.Where(r => r.CreatedAt >= month && r.CreatedAt < monthEnd).ToList();

                decimal SumFor(string type) =>
                    monthRows.Where(r => ResolveType(r.CreditQuantity, r.TransactionType, r.CandidateId, r.EmployerId) == type)
                             .Sum(r => (decimal)r.TotalAmountPaise) / 100m;

                labels.Add(month.ToString("MMM"));
                candidateSeries.Add(SumFor("candidate"));
                recruiterSeries.Add(SumFor("recruiter"));
                creditSeries.Add(SumFor("credits"));
            }

            return new RevenueCreditGrowthResponseDto
            {
                Labels = labels,
                CandidateMemberships = candidateSeries,
                RecruiterMemberships = recruiterSeries,
                CreditPlans = creditSeries
            };
        }

        // Same classification rule used on /admin/revenue: credit-plan
        // purchases first, then whichever party (candidate/employer) the
        // row belongs to.
        private static string ResolveType(int? creditQuantity, string? transactionType, Guid? candidateId, Guid? employerId)
        {
            var isCredits =
                creditQuantity.HasValue ||
                (!string.IsNullOrEmpty(transactionType) &&
                 transactionType.Contains("Credit", StringComparison.OrdinalIgnoreCase));

            if (isCredits) return "credits";
            if (candidateId.HasValue) return "candidate";
            if (employerId.HasValue) return "recruiter";
            return "recruiter";
        }

        // ------------------------------------------------------------
        // 5. PLATFORM OVERVIEW
        // ------------------------------------------------------------
        public async Task<PlatformOverviewResponseDto> GetPlatformOverviewAsync()
        {
            var now = DateTime.UtcNow;
            var last24h = now.AddHours(-24);

            // ---- Plans ----
            var membershipPlans = await _db.MembershipPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => p.PlanType)
                .ToListAsync();
            var activeCreditPlans = await _db.CreditPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .CountAsync();

            var recruiterPlanCount = membershipPlans.Count(p => p == PlanType.Recruiter);
            var candidatePlanCount = membershipPlans.Count(p => p == PlanType.Candidate);

            // ---- Admin users ----
            var adminUsers = await _db.AdminUsers
                .AsNoTracking()
                .Select(a => a.IsActive)
                .ToListAsync();

            // ---- Audit logs ----
            var auditRows = await _db.AuditLogs
                .AsNoTracking()
                .Where(a => a.CreatedAt >= last24h)
                .Select(a => a.Severity)
                .ToListAsync();

            // ---- Legal pages ----
            var legalDocs = await _db.LegalDocuments
                .AsNoTracking()
                .Select(d => new { d.Status, d.PublishedAt })
                .ToListAsync();

            return new PlatformOverviewResponseDto
            {
                Plans = new PlansOverviewDto
                {
                    ActiveCount = recruiterPlanCount + candidatePlanCount + activeCreditPlans,
                    RecruiterPlanCount = recruiterPlanCount,
                    CandidatePlanCount = candidatePlanCount,
                    CreditPlanCount = activeCreditPlans
                },
                Users = new UsersOverviewDto
                {
                    Total = adminUsers.Count,
                    Active = adminUsers.Count(a => a),
                    Inactive = adminUsers.Count(a => !a)
                },
                AuditLogs = new AuditOverviewDto
                {
                    CriticalLast24Hours = auditRows.Count(s => s == AuditSeverity.Critical),
                    TotalLast24Hours = auditRows.Count
                },
                LegalPages = new LegalPagesOverviewDto
                {
                    TotalDocuments = legalDocs.Count,
                    PublishedCount = legalDocs.Count(d => d.Status == "Published"),
                    LastPublishedAt = legalDocs
                        .Where(d => d.PublishedAt.HasValue)
                        .Select(d => d.PublishedAt)
                        .OrderByDescending(d => d)
                        .FirstOrDefault()
                }
            };
        }

        // ------------------------------------------------------------
        // 6. RECENT REGISTRATIONS
        // ------------------------------------------------------------
        public async Task<List<RecentRegistrationDto>> GetRecentRegistrationsAsync(int limit)
        {
            limit = NormalizeLimit(limit);

            var candidateRows = await _db.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.UserType == UserType.Candidate)
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit)
                .Select(u => new
                {
                    u.UserId,
                    u.CreatedAt,
                    Name = _db.CandidateProfiles
                        .Where(c => c.UserId == u.UserId)
                        .Select(c => c.FullName)
                        .FirstOrDefault(),
                    CandidateId = _db.CandidateProfiles
                        .Where(c => c.UserId == u.UserId)
                        .Select(c => (Guid?)c.CandidateId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var recruiterRows = await _db.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.UserType == UserType.Recruiter)
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit)
                .Select(u => new
                {
                    u.UserId,
                    u.CreatedAt,
                    ContactName = _db.EmployerProfiles
                        .Where(e => e.UserId == u.UserId)
                        .Select(e => e.ContactPersonName)
                        .FirstOrDefault(),
                    CompanyName = _db.EmployerProfiles
                        .Where(e => e.UserId == u.UserId)
                        .Select(e => e.CompanyDisplayName)
                        .FirstOrDefault(),
                    EmployerId = _db.EmployerProfiles
                        .Where(e => e.UserId == u.UserId)
                        .Select(e => (Guid?)e.EmployerId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var combined = new List<RecentRegistrationDto>();

            combined.AddRange(candidateRows.Select(r => new RecentRegistrationDto
            {
                UserId = r.UserId,
                Type = "candidate",
                Name = string.IsNullOrWhiteSpace(r.Name) ? "Unnamed candidate" : r.Name,
                CreatedAt = r.CreatedAt,
                CandidateId = r.CandidateId
            }));

            combined.AddRange(recruiterRows.Select(r => new RecentRegistrationDto
            {
                UserId = r.UserId,
                Type = "recruiter",
                Name = !string.IsNullOrWhiteSpace(r.ContactName)
                    ? r.ContactName!
                    : (!string.IsNullOrWhiteSpace(r.CompanyName) ? r.CompanyName! : "Unnamed recruiter"),
                CreatedAt = r.CreatedAt,
                EmployerId = r.EmployerId
            }));

            return combined
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToList();
        }

        // ------------------------------------------------------------
        // 7. RECENT SUPPORT TICKETS
        // ------------------------------------------------------------
        public async Task<List<RecentSupportTicketDto>> GetRecentSupportTicketsAsync(int limit)
        {
            limit = NormalizeLimit(limit);

            var rows = await _db.SupportTickets
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new
                {
                    t.TicketId,
                    t.RaisedBy,
                    t.Subject,
                    t.TicketType,
                    t.Status,
                    t.CreatedAt
                })
                .ToListAsync();

            var raisedByIds = rows.Select(r => r.RaisedBy).Distinct().ToList();

            var candidateNames = await _db.CandidateProfiles
                .AsNoTracking()
                .Where(c => raisedByIds.Contains(c.UserId))
                .Select(c => new { c.UserId, c.FullName })
                .ToListAsync();

            var recruiterNames = await _db.EmployerProfiles
                .AsNoTracking()
                .Where(e => raisedByIds.Contains(e.UserId))
                .Select(e => new { e.UserId, e.ContactPersonName })
                .ToListAsync();

            return rows.Select(r =>
            {
                var name = candidateNames.FirstOrDefault(c => c.UserId == r.RaisedBy)?.FullName
                    ?? recruiterNames.FirstOrDefault(e => e.UserId == r.RaisedBy)?.ContactPersonName;

                return new RecentSupportTicketDto
                {
                    TicketId = r.TicketId,
                    RaisedByName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name!,
                    Subject = r.Subject,
                    Category = ResolveTicketCategory(r.TicketType),
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();
        }

        private static string ResolveTicketCategory(SupportTicketType type) => type switch
        {
            SupportTicketType.ProfileAndResume => "Profile",
            SupportTicketType.JobApplication => "Job Application",
            SupportTicketType.PaymentAndBilling => "Billing",
            SupportTicketType.AccountAccess => "Account Access",
            SupportTicketType.TechnicalIssue => "Technical",
            _ => "Other"
        };

        // ------------------------------------------------------------
        // 8. RECENT PAYMENTS
        // ------------------------------------------------------------
        public async Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(int limit)
        {
            limit = NormalizeLimit(limit);

            var rows = await _db.PaymentTransactions
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new
                {
                    t.TransactionId,
                    t.TotalAmountPaise,
                    t.PaymentStatus,
                    t.CreatedAt,
                    EntityName = t.EmployerProfile != null
                        ? t.EmployerProfile.CompanyDisplayName
                        : (t.CandidateProfile != null ? t.CandidateProfile.FullName : null)
                })
                .ToListAsync();

            return rows.Select(r => new RecentPaymentDto
            {
                TransactionId = r.TransactionId,
                EntityName = string.IsNullOrWhiteSpace(r.EntityName) ? "Unknown" : r.EntityName!,
                Amount = r.TotalAmountPaise / 100m,
                PaymentStatus = r.PaymentStatus,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        // ------------------------------------------------------------
        // SHARED HELPERS
        // ------------------------------------------------------------
        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0) return 5;
            return limit > 50 ? 50 : limit;
        }
    }
}