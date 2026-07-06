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

        [HttpGet("invoices")]
        public async Task<IActionResult>
            GetInvoices(
                [FromHeader(Name = "EmployerId")]
                Guid employerId,

                [FromQuery]
                DateOnly? fromDate,

                [FromQuery]
                DateOnly? toDate)
        {
            var result =
                await _service.GetInvoicesAsync(
                    employerId,
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
    }
}