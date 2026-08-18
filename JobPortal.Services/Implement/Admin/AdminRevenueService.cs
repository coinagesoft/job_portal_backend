using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
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
        // Filters removed for QA testing — always all-time, all-country.
        public async Task<RevenueSummaryDto> GetSummaryAsync()
        {
            string? country = null;
            DateOnly? dateFrom = null;
            DateOnly? dateTo = null;

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
        // Filters removed for QA testing — always the current calendar
        // month ("monthly"), all countries.
        public async Task<RevenueByCountryDto> GetRevenueByCountryAsync()
        {
            const string period = "monthly";
            string? country = null;

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
        // Filters (type/country/search/date range) removed for QA testing —
        // returns every completed transaction, newest first. Pagination is
        // kept (it isn't a data filter).
        public async Task<RevenueTransactionsResponseDto> GetTransactionsAsync(
            int page,
            int pageSize)
        {
            const string type = "all";
            string? country = null;
            string? search = null;
            DateOnly? dateFrom = null;
            DateOnly? dateTo = null;
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

                // The PDF is generated on demand (see DownloadInvoicePdfAsync)
                // rather than stored, so this just signals to the frontend
                // that a downloadable invoice exists for this transaction —
                // same pattern used on the employer/recruiter side.
                InvoiceUrl = r.InvoiceNumber != null
                    ? $"/api/admin/revenue/transactions/{r.TransactionId}/invoice/download"
                    : null
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
                .Select(i => new { i.InvoiceNumber, i.InvoiceDate })
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
                InvoiceUrl = invoice?.InvoiceNumber != null
                    ? $"/api/admin/revenue/transactions/{transactionId}/invoice/download"
                    : null
            };
        }

        // ------------------------------------------------------------
        // INVOICE PDF (generated on demand — see RecruiterInvoiceService
        // for the employer-side twin of this; kept separate here since
        // admin transactions can be billed to either an employer or a
        // candidate, and admin has no per-user auth scoping to apply).
        // ------------------------------------------------------------
        public async Task<(byte[] Bytes, string FileName)?> DownloadInvoicePdfAsync(
            Guid transactionId)
        {
            var data = await (
                from invoice in _db.Invoices
                join transaction in _db.PaymentTransactions
                on invoice.TransactionId equals transaction.TransactionId
                where invoice.TransactionId == transactionId
                select new { Invoice = invoice, Transaction = transaction }
            ).AsNoTracking().FirstOrDefaultAsync();

            if (data == null)
            {
                return null;
            }

            EmployerProfile? employer = null;
            CandidateProfile? candidate = null;
            string? contactEmail = null;

            if (data.Transaction.EmployerId.HasValue)
            {
                employer = await _db.EmployerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployerId == data.Transaction.EmployerId.Value);

                contactEmail = !string.IsNullOrWhiteSpace(employer?.ContactEmailPublic)
                    ? employer!.ContactEmailPublic
                    : await _db.Users
                        .Where(u => u.UserId == data.Transaction.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();
            }
            else if (data.Transaction.CandidateId.HasValue)
            {
                candidate = await _db.CandidateProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CandidateId == data.Transaction.CandidateId.Value);

                contactEmail = await _db.Users
                    .Where(u => u.UserId == data.Transaction.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
            }

            var bytes = BuildInvoicePdf(data.Invoice, data.Transaction, employer, candidate, contactEmail);
            var fileName = $"{data.Invoice.InvoiceNumber}.pdf";

            return (bytes, fileName);
        }

        private static byte[] BuildInvoicePdf(
            Invoice invoice,
            PaymentTransaction transaction,
            EmployerProfile? employer,
            CandidateProfile? candidate,
            string? contactEmail)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);
            document.SetMargins(36, 36, 36, 36);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var brandColor = new DeviceRgb(230, 126, 24);

            // ── Header ──────────────────────────────────────────
            document.Add(
                new Paragraph("JobBox")
                    .SetFont(titleFont)
                    .SetFontSize(22)
                    .SetFontColor(brandColor)
                    .SetMarginBottom(0));

            document.Add(
                new Paragraph("TAX INVOICE")
                    .SetFont(titleFont)
                    .SetFontSize(13)
                    .SetMarginTop(2)
                    .SetMarginBottom(16));

            // ── Invoice meta ────────────────────────────────────
            var metaTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(14);

            metaTable.AddCell(PlainCell($"Invoice No: {invoice.InvoiceNumber}", regularFont));
            metaTable.AddCell(PlainCell($"Invoice Date: {invoice.InvoiceDate:dd MMM yyyy}", regularFont));
            metaTable.AddCell(PlainCell(
                $"Payment Ref: {transaction.RazorpayPaymentId ?? transaction.RazorpayOrderId ?? "-"}",
                regularFont));
            metaTable.AddCell(PlainCell($"Status: {transaction.PaymentStatus}", regularFont));
            document.Add(metaTable);

            // ── Billed to ───────────────────────────────────────
            document.Add(new Paragraph("Billed To").SetFont(titleFont).SetFontSize(11).SetMarginBottom(2));
            document.Add(new Paragraph(employer?.CompanyDisplayName ?? candidate?.FullName ?? "-")
                .SetFont(regularFont).SetFontSize(10));

            if (employer != null)
            {
                var addressLine = string.Join(", ", new[]
                {
                    employer.AddressLine1,
                    employer.AddressLine2,
                    employer.City,
                    employer.State,
                    employer.Pincode,
                    employer.Country
                }.Where(part => !string.IsNullOrWhiteSpace(part)));

                if (!string.IsNullOrWhiteSpace(addressLine))
                {
                    document.Add(new Paragraph(addressLine).SetFont(regularFont).SetFontSize(9));
                }

                if (employer.GstRegistered && !string.IsNullOrWhiteSpace(employer.Gstin))
                {
                    document.Add(new Paragraph($"GSTIN: {employer.Gstin}").SetFont(regularFont).SetFontSize(9));
                }

                if (!string.IsNullOrWhiteSpace(employer.Pan))
                {
                    document.Add(new Paragraph($"PAN: {employer.Pan}").SetFont(regularFont).SetFontSize(9));
                }

                if (!string.IsNullOrWhiteSpace(employer.ContactPhone))
                {
                    document.Add(new Paragraph($"Phone: {employer.ContactPhone}").SetFont(regularFont).SetFontSize(9));
                }
            }
            else if (candidate != null)
            {
                var addressLine = string.Join(", ", new[]
                {
                    candidate.CurrentCity,
                    candidate.CurrentState,
                    candidate.Pincode,
                    candidate.Nationality
                }.Where(part => !string.IsNullOrWhiteSpace(part)));

                if (!string.IsNullOrWhiteSpace(addressLine))
                {
                    document.Add(new Paragraph(addressLine).SetFont(regularFont).SetFontSize(9));
                }
            }

            if (!string.IsNullOrWhiteSpace(contactEmail))
            {
                document.Add(new Paragraph($"Email: {contactEmail}").SetFont(regularFont).SetFontSize(9));
            }

            // ── Line item ───────────────────────────────────────
            var itemsTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 2, 2 }))
                .UseAllAvailableWidth()
                .SetMarginTop(20);

            itemsTable.AddHeaderCell(HeaderCell("Description", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Validity", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Amount (Rs.)", titleFont));

            itemsTable.AddCell(BodyCell(
                $"{transaction.PackType ?? transaction.TransactionType ?? "Plan"} ({transaction.CreditQuantity ?? 0} credits)",
                regularFont));
            itemsTable.AddCell(BodyCell($"{transaction.ValidityMonths ?? 0} month(s)", regularFont));
            itemsTable.AddCell(BodyCell(invoice.InvoiceAmount.ToString("N2"), regularFont));

            document.Add(itemsTable);

            // ── Totals ──────────────────────────────────────────
            var totalsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1 }))
                .UseAllAvailableWidth()
                .SetMarginTop(10);

            totalsTable.AddCell(TotalsLabelCell("Subtotal", regularFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceAmount:N2}", regularFont));

            totalsTable.AddCell(TotalsLabelCell("GST (18%)", regularFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceGst:N2}", regularFont));

            totalsTable.AddCell(TotalsLabelCell("Total", titleFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceTotal:N2}", titleFont));

            document.Add(totalsTable);

            document.Add(
                new Paragraph("\nThis is a system-generated invoice and does not require a signature.")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginTop(24));

            document.Close();
            return stream.ToArray();
        }

        private static Cell PlainCell(string text, PdfFont font) =>
            new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

        private static Cell HeaderCell(string text, PdfFont font) =>
            new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBackgroundColor(new DeviceRgb(245, 245, 245));

        private static Cell BodyCell(string text, PdfFont font) =>
            new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(9));

        private static Cell TotalsLabelCell(string text, PdfFont font) =>
            new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);

        private static Cell TotalsValueCell(string text, PdfFont font) =>
            new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);

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