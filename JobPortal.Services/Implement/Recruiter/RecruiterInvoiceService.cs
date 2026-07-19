using Google;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto;
using JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto.cs;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterInvoiceService :
        IRecruiterInvoiceService
    {
        private readonly AppDbContext _context;

        public RecruiterInvoiceService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployerInvoiceDto>>
            GetInvoicesAsync(
                Guid employerId,
                DateOnly? fromDate,
                DateOnly? toDate)
        {
            var query =
                from invoice in _context.Invoices

                join transaction in _context.PaymentTransactions
                on invoice.TransactionId equals transaction.TransactionId

                where transaction.EmployerId == employerId

                select new
                {
                    Invoice = invoice,
                    Transaction = transaction
                };

            if (fromDate.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Invoice.InvoiceDate >=
                        fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Invoice.InvoiceDate <=
                        toDate.Value);
            }

            return await query
                .OrderByDescending(x =>
                    x.Invoice.InvoiceDate)
                .Select(x =>
                    new EmployerInvoiceDto
                    {
                        InvoiceId =
                            x.Invoice.InvoiceId,

                        InvoiceNumber =
                            x.Invoice.InvoiceNumber,

                        InvoiceDate =
                            x.Invoice.InvoiceDate,

                        TransactionType =
                            x.Transaction.TransactionType,

                        Amount =
                            x.Invoice.InvoiceAmount,

                        Gst =
                            x.Invoice.InvoiceGst,

                        Total =
                            x.Invoice.InvoiceTotal,

                        // The PDF is generated on demand (see DownloadInvoicePdfAsync)
                        // rather than stored, so every invoice row can always be
                        // downloaded — this just signals that to the frontend.
                        InvoiceUrl =
                            $"/api/recruiter/invoices/{x.Invoice.InvoiceId}/download"
                    })
                .ToListAsync();
        }

        public async Task<InvoiceDownloadDto?>
            GetInvoiceAsync(
                Guid invoiceId)
        {
            return await _context.Invoices
                .Where(x =>
                    x.InvoiceId == invoiceId)
                .Select(x =>
                    new InvoiceDownloadDto
                    {
                        InvoiceId =
                            x.InvoiceId,

                        InvoiceNumber =
                            x.InvoiceNumber,

                        InvoiceUrl =
                            x.InvoiceS3Url
                    })
                .FirstOrDefaultAsync();
        }

        // ────────────────────────────────────────────────────────────
        // Generates a GST-compliant invoice PDF in memory and returns
        // it for streaming straight to the browser. Nothing is stored
        // on disk / cloud — regenerated fresh on every request.
        // ────────────────────────────────────────────────────────────
        public async Task<(byte[] Bytes, string FileName)?>
            DownloadInvoicePdfAsync(
                Guid invoiceId,
                Guid employerId)
        {
            var data = await (
                from invoice in _context.Invoices

                join transaction in _context.PaymentTransactions
                on invoice.TransactionId equals transaction.TransactionId

                where invoice.InvoiceId == invoiceId
                      && transaction.EmployerId == employerId

                select new { Invoice = invoice, Transaction = transaction }
            ).FirstOrDefaultAsync();

            if (data == null)
            {
                return null;
            }

            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            var bytes = BuildInvoicePdf(data.Invoice, data.Transaction, employer);
            var fileName = $"{data.Invoice.InvoiceNumber}.pdf";

            return (bytes, fileName);
        }

        private static byte[] BuildInvoicePdf(
            Invoice invoice,
            PaymentTransaction transaction,
            EmployerProfile? employer)
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
            document.Add(new Paragraph(employer?.CompanyDisplayName ?? "-").SetFont(regularFont).SetFontSize(10));

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
            }

            // ── Line item ───────────────────────────────────────
            var itemsTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 2, 2 }))
                .UseAllAvailableWidth()
                .SetMarginTop(20);

            itemsTable.AddHeaderCell(HeaderCell("Description", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Validity", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Amount (Rs.)", titleFont));

            itemsTable.AddCell(BodyCell(
                $"{transaction.PackType ?? "Credit Plan"} ({transaction.CreditQuantity ?? 0} credits)",
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
    }
}