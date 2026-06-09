using Google;
using JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto;
using JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto.cs;
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

                        InvoiceUrl =
                            x.Invoice.InvoiceS3Url
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
    }
}