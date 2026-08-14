using JobPortal.Application.DTOs.Admin.Revenue;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    // Powers Admin ▸ Revenue (https://.../admin/revenue) only.
    //
    // "Revenue" here means completed (paid) PaymentTransaction rows.
    // Each transaction is bucketed into one of 3 categories purely from
    // data already on the row — no hardcoded list of TransactionType
    // strings — so new transaction types (e.g. a future recruiter
    // membership purchase flow) fall into "recruiter"/"candidate"
    // automatically instead of silently being left uncategorized:
    //   - "credits"   → CreditQuantity is set, or TransactionType
    //                    mentions "Credit" (credit-plan purchases)
    //   - "candidate" → CandidateId is set and it isn't a credit txn
    //   - "recruiter" → EmployerId is set and it isn't a credit txn
    public class AdminRevenueService : IAdminRevenueService
    {
        private const string CompletedStatus = "Completed";

        private readonly AppDbContext _db;

        public AdminRevenueService(AppDbContext db)
        {
            _db = db;
        }

        // Country name → short badge code, matching the codes already
        // used across the admin panel's country filter/table. Falls
        // back to the first 3 letters of the country name for anything
        // not in this list, so unmapped countries still render sanely.
        private static readonly Dictionary<string, string> CountryCodeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["United States"] = "USA",
                ["India"] = "IND",
                ["United Kingdom"] = "GBR",
                ["Australia"] = "AUS",
                ["United Arab Emirates"] = "UAE",
                ["Saudi Arabia"] = "KSA",
                ["Qatar"] = "QAT",
                ["Kuwait"] = "KWT",
                ["Bahrain"] = "BHR",
                ["Oman"] = "OMN",
                ["Egypt"] = "EGY",
                ["Jordan"] = "JOR",
                ["Lebanon"] = "LBN",
                ["Turkey"] = "TUR",
            };

        private static string ResolveCountryCode(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return "N/A";

            if (CountryCodeMap.TryGetValue(country, out var code))
                return code;

            return country.Length >= 3
                ? country.Substring(0, 3).ToUpperInvariant()
                : country.ToUpperInvariant();
        }

        private static string ResolveType(int? creditQuantity, string? transactionType, Guid? candidateId, Guid? employerId)
        {
            var isCredits =
                creditQuantity.HasValue ||
                (!string.IsNullOrEmpty(transactionType) &&
                 transactionType.Contains("Credit", StringComparison.OrdinalIgnoreCase));

            if (isCredits) return "credits";
            if (candidateId.HasValue) return "candidate";
            if (employerId.HasValue) return "recruiter";
            return "recruiter"; // shouldn't happen, but never leave a row unclassified
        }

        // ------------------------------------------------------------
        // SUMMARY CARDS
        // ------------------------------------------------------------
        public async Task<RevenueSummaryDto> GetSummaryAsync(
            string? country,
            DateOnly? dateFrom,
            DateOnly? dateTo)
        {
            country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();

            var (start, end) = ResolveDateWindow(dateFrom, dateTo);

            var rows = await BaseCompletedQuery(country, start, end)
                .Select(t => new
                {
                    t.TotalAmountPaise,
                    t.CreditQuantity,
                    t.TransactionType,
                    t.CandidateId,
                    t.EmployerId
                })
                .ToListAsync();

            decimal SumFor(Func<string, bool> predicate) =>
                rows.Where(r => predicate(ResolveType(r.CreditQuantity, r.TransactionType, r.CandidateId, r.EmployerId)))
                    .Sum(r => r.TotalAmountPaise) / 100m;

            var totalRevenue = rows.Sum(r => (decimal)r.TotalAmountPaise) / 100m;
            var candidateRevenue = SumFor(t => t == "candidate");
            var recruiterRevenue = SumFor(t => t == "recruiter");
            var creditsRevenue = SumFor(t => t == "credits");

            decimal? PercentOf(decimal amount) =>
                totalRevenue > 0 ? Math.Round(amount / totalRevenue * 100, 1) : 0;

            // "vs last month" only makes sense against the default
            // (no explicit date range) window — a custom range has no
            // single well-defined "previous period".
            decimal? totalChangePercent = null;
            if (dateFrom is null && dateTo is null)
            {
                var now = DateTime.UtcNow;
                var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var previousMonthStart = currentMonthStart.AddMonths(-1);

                var previousMonthTotal = await BaseCompletedQuery(country, previousMonthStart, currentMonthStart)
                    .SumAsync(t => (decimal?)t.TotalAmountPaise) ?? 0m;
                previousMonthTotal /= 100m;

                if (previousMonthTotal > 0)
                {
                    totalChangePercent = Math.Round(
                        (totalRevenue - previousMonthTotal) / previousMonthTotal * 100, 1);
                }
                else if (totalRevenue > 0)
                {
                    totalChangePercent = 100m; // went from nothing to something
                }
            }

            return new RevenueSummaryDto
            {
                TotalRevenue = new RevenueSummaryCardDto
                {
                    Amount = totalRevenue,
                    PercentOfTotal = null,
                    ChangePercentVsPrevious = totalChangePercent
                },
                CandidateMemberships = new RevenueSummaryCardDto
                {
                    Amount = candidateRevenue,
                    PercentOfTotal = PercentOf(candidateRevenue)
                },
                RecruiterMemberships = new RevenueSummaryCardDto
                {
                    Amount = recruiterRevenue,
                    PercentOfTotal = PercentOf(recruiterRevenue)
                },
                CreditPlans = new RevenueSummaryCardDto
                {
                    Amount = creditsRevenue,
                    PercentOfTotal = PercentOf(creditsRevenue)
                }
            };
        }

        // ------------------------------------------------------------
        // REVENUE BY COUNTRY + COMPOSITION
        // ------------------------------------------------------------
        public async Task<RevenueByCountryDto> GetRevenueByCountryAsync(
            string period,
            string? country)
        {
            period = string.Equals(period, "yearly", StringComparison.OrdinalIgnoreCase)
                ? "yearly"
                : "monthly";

            country = string.IsNullOrWhiteSpace(country) || country.Equals("All countries", StringComparison.OrdinalIgnoreCase)
                ? null
                : country.Trim();

            var now = DateTime.UtcNow;
            var start = period == "yearly"
                ? new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = period == "yearly" ? start.AddYears(1) : start.AddMonths(1);

            var rows = await BaseCompletedQuery(country, start, end)
                .Select(t => new
                {
                    Country = t.EmployerProfile != null
                        ? t.EmployerProfile.Country
                        : (t.CandidateProfile != null ? t.CandidateProfile.Nationality : null),
                    t.TotalAmountPaise,
                    t.CreditQuantity,
                    t.TransactionType,
                    t.CandidateId,
                    t.EmployerId
                })
                .ToListAsync();

            var totalAmount = rows.Sum(r => r.TotalAmountPaise) / 100m;

            var countries = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Country) ? "Unknown" : r.Country!)
                .Select(g =>
                {
                    var amount = g.Sum(x => x.TotalAmountPaise) / 100m;
                    return new RevenueCountryRowDto
                    {
                        Country = g.Key,
                        CountryCode = ResolveCountryCode(g.Key),
                        Amount = amount,
                        PercentOfTotal = totalAmount > 0
                            ? Math.Round(amount / totalAmount * 100, 1)
                            : 0
                    };
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            decimal PercentFor(string type)
            {
                var amount = rows
                    .Where(r => ResolveType(r.CreditQuantity, r.TransactionType, r.CandidateId, r.EmployerId) == type)
                    .Sum(r => r.TotalAmountPaise) / 100m;

                return totalAmount > 0 ? Math.Round(amount / totalAmount * 100, 1) : 0;
            }

            return new RevenueByCountryDto
            {
                Period = period,
                TotalAmount = totalAmount,
                Countries = countries,
                Composition = new RevenueCompositionDto
                {
                    CandidatePercent = PercentFor("candidate"),
                    RecruiterPercent = PercentFor("recruiter"),
                    CreditsPercent = PercentFor("credits")
                }
            };
        }

        // ------------------------------------------------------------
        // TRANSACTIONS TABLE
        // ------------------------------------------------------------
        public async Task<RevenueTransactionsResponseDto> GetTransactionsAsync(
            string type,
            string? country,
            string? search,
            DateOnly? dateFrom,
            DateOnly? dateTo,
            int page,
            int pageSize)
        {
            type = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLowerInvariant();
            country = string.IsNullOrWhiteSpace(country) || country.Equals("All countries", StringComparison.OrdinalIgnoreCase)
                ? null
                : country.Trim();
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

            var (start, end) = ResolveDateWindow(dateFrom, dateTo);

            var query = BaseCompletedQuery(country, start, end);

            query = type switch
            {
                "credits" => query.Where(t =>
                    t.CreditQuantity != null ||
                    (t.TransactionType != null && t.TransactionType.Contains("Credit"))),

                "candidate" => query.Where(t =>
                    t.CandidateId != null &&
                    t.CreditQuantity == null &&
                    (t.TransactionType == null || !t.TransactionType.Contains("Credit"))),

                "recruiter" => query.Where(t =>
                    t.EmployerId != null &&
                    t.CreditQuantity == null &&
                    (t.TransactionType == null || !t.TransactionType.Contains("Credit"))),

                _ => query
            };

            var projected = query.Select(t => new
            {
                t.TransactionId,
                t.CreatedAt,
                t.PackType,
                t.TransactionType,
                t.TotalAmountPaise,
                t.PaymentMethod,
                t.PaymentStatus,
                t.CreditQuantity,
                t.CandidateId,
                t.EmployerId,
                CustomerName = t.EmployerProfile != null
                    ? t.EmployerProfile.CompanyDisplayName
                    : (t.CandidateProfile != null ? t.CandidateProfile.FullName : null),
                Country = t.EmployerProfile != null
                    ? t.EmployerProfile.Country
                    : (t.CandidateProfile != null ? t.CandidateProfile.Nationality : null),
                InvoiceNumber = _db.Invoices
                    .Where(i => i.TransactionId == t.TransactionId)
                    .Select(i => i.InvoiceNumber)
                    .FirstOrDefault(),
                InvoiceDate = _db.Invoices
                    .Where(i => i.TransactionId == t.TransactionId)
                    .Select(i => (DateOnly?)i.InvoiceDate)
                    .FirstOrDefault(),
                InvoiceUrl = _db.Invoices
                    .Where(i => i.TransactionId == t.TransactionId)
                    .Select(i => i.InvoiceS3Url)
                    .FirstOrDefault()
            });

            if (search != null)
            {
                projected = projected.Where(t =>
                    EF.Functions.Like(t.TransactionId.ToString(), $"%{search}%") ||
                    (t.CustomerName != null && EF.Functions.Like(t.CustomerName, $"%{search}%")) ||
                    (t.InvoiceNumber != null && EF.Functions.Like(t.InvoiceNumber, $"%{search}%")));
            }

            var totalCount = await projected.CountAsync();

            var pageRows = await projected
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pageRows.Select(r => new RevenueTransactionDto
            {
                TransactionId = r.TransactionId,
                Date = r.CreatedAt,
                Customer = r.CustomerName ?? "Unknown",
                Plan = !string.IsNullOrWhiteSpace(r.PackType) ? r.PackType! : r.TransactionType,
                Type = ResolveType(r.CreditQuantity, r.TransactionType, r.CandidateId, r.EmployerId),
                Country = string.IsNullOrWhiteSpace(r.Country) ? "Unknown" : r.Country!,
                CountryCode = ResolveCountryCode(r.Country),
                Amount = r.TotalAmountPaise / 100m,
                PaymentMethod = r.PaymentMethod,
                PaymentStatus = r.PaymentStatus,
                InvoiceNumber = r.InvoiceNumber,
                InvoiceDate = r.InvoiceDate,
                InvoiceUrl = r.InvoiceUrl
            }).ToList();

            return new RevenueTransactionsResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        // ------------------------------------------------------------
        // SINGLE TRANSACTION / INVOICE DETAIL (for the invoice modal)
        // ------------------------------------------------------------
        public async Task<RevenueTransactionDto?> GetTransactionInvoiceAsync(Guid transactionId)
        {
            var t = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(x => x.TransactionId == transactionId && x.PaymentStatus == CompletedStatus)
                .Select(x => new
                {
                    x.TransactionId,
                    x.CreatedAt,
                    x.PackType,
                    x.TransactionType,
                    x.TotalAmountPaise,
                    x.PaymentMethod,
                    x.PaymentStatus,
                    x.CreditQuantity,
                    x.CandidateId,
                    x.EmployerId,
                    CustomerName = x.EmployerProfile != null
                        ? x.EmployerProfile.CompanyDisplayName
                        : (x.CandidateProfile != null ? x.CandidateProfile.FullName : null),
                    Country = x.EmployerProfile != null
                        ? x.EmployerProfile.Country
                        : (x.CandidateProfile != null ? x.CandidateProfile.Nationality : null)
                })
                .FirstOrDefaultAsync();

            if (t == null) return null;

            var invoice = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.TransactionId == transactionId)
                .Select(i => new { i.InvoiceNumber, i.InvoiceDate, i.InvoiceS3Url })
                .FirstOrDefaultAsync();

            return new RevenueTransactionDto
            {
                TransactionId = t.TransactionId,
                Date = t.CreatedAt,
                Customer = t.CustomerName ?? "Unknown",
                Plan = !string.IsNullOrWhiteSpace(t.PackType) ? t.PackType! : t.TransactionType,
                Type = ResolveType(t.CreditQuantity, t.TransactionType, t.CandidateId, t.EmployerId),
                Country = string.IsNullOrWhiteSpace(t.Country) ? "Unknown" : t.Country!,
                CountryCode = ResolveCountryCode(t.Country),
                Amount = t.TotalAmountPaise / 100m,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                InvoiceNumber = invoice?.InvoiceNumber,
                InvoiceDate = invoice?.InvoiceDate,
                InvoiceUrl = invoice?.InvoiceS3Url
            };
        }

        // ------------------------------------------------------------
        // SHARED HELPERS
        // ------------------------------------------------------------

        // Turns the page's optional From/To date filters into a UTC
        // [start, end) window. When neither is supplied, returns
        // (null, null) meaning "no date restriction".
        private static (DateTime? start, DateTime? end) ResolveDateWindow(DateOnly? dateFrom, DateOnly? dateTo)
        {
            DateTime? start = dateFrom.HasValue
                ? dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : null;

            // Inclusive of the whole "to" day.
            DateTime? end = dateTo.HasValue
                ? dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : null;

            return (start, end);
        }

        private IQueryable<PaymentTransaction> BaseCompletedQuery(
            string? country,
            DateTime? start,
            DateTime? end)
        {
            var query = _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.PaymentStatus == CompletedStatus);

            if (start.HasValue)
                query = query.Where(t => t.CreatedAt >= start.Value);

            if (end.HasValue)
                query = query.Where(t => t.CreatedAt < end.Value);

            if (!string.IsNullOrWhiteSpace(country) && !country.Equals("All countries", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t =>
                    (t.EmployerProfile != null && t.EmployerProfile.Country == country) ||
                    (t.CandidateProfile != null && t.CandidateProfile.Nationality == country));
            }

            return query;
        }
    }
}