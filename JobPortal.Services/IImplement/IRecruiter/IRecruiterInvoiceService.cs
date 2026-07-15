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
    }
}