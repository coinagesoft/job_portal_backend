using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterInvoiceController :
        ControllerBase
    {
        private readonly IRecruiterInvoiceService _service;

        public RecruiterInvoiceController(
            IRecruiterInvoiceService service)
        {
            _service = service;
        }

        // EmployerId is resolved from the signed JWT rather than a
        // client-supplied header — the token already carries it for both
        // the account owner and any of their sub-users (see
        // RecruiterAuthService.GenerateUserTokenAsync).
        private Guid GetEmployerId()
        {
            var employerId = User.FindFirst("EmployerId")?.Value;

            if (string.IsNullOrWhiteSpace(employerId))
                throw new UnauthorizedAccessException(
                    "Employer ID not found in token.");

            return Guid.Parse(employerId);
        }

        [HttpGet("invoices")]
        public async Task<IActionResult>
            GetInvoices(
                [FromQuery]
                DateOnly? fromDate,

                [FromQuery]
                DateOnly? toDate)
        {
            var result =
                await _service.GetInvoicesAsync(
                    GetEmployerId(),
                    fromDate,
                    toDate);

            return Ok(result);
        }

        [HttpGet("invoices/{invoiceId}")]
        public async Task<IActionResult>
            GetInvoice(
                Guid invoiceId)
        {
            var result =
                await _service.GetInvoiceAsync(
                    invoiceId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // GET /api/recruiter/invoices/{invoiceId}/download
        // Streams a freshly-generated, GST-compliant invoice PDF.
        [HttpGet("invoices/{invoiceId:guid}/download")]
        public async Task<IActionResult>
            DownloadInvoicePdf(
                Guid invoiceId)
        {
            var result =
                await _service.DownloadInvoicePdfAsync(
                    invoiceId,
                    GetEmployerId());

            if (result == null)
            {
                return NotFound(new { success = false, message = "Invoice not found." });
            }

            return File(result.Value.Bytes, "application/pdf", result.Value.FileName);
        }
    }
}