using JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto.cs;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterInvoiceService
    {
        Task<List<EmployerInvoiceDto>>
            GetInvoicesAsync(
                Guid employerId,
                DateOnly? fromDate,
                DateOnly? toDate);

        Task<InvoiceDownloadDto?>
            GetInvoiceAsync(
                Guid invoiceId);

        Task<(byte[] Bytes, string FileName)?>
            DownloadInvoicePdfAsync(
                Guid invoiceId,
                Guid employerId);

        // Regenerates the invoice PDF and emails it to the employer's
        // contact email as an attachment. Used both for the manual
        // "Email Invoice" action and automatically right after a
        // successful credit plan purchase.
        Task<(bool Success, string Message)>
            EmailInvoiceAsync(
                Guid invoiceId,
                Guid employerId);
    }
}