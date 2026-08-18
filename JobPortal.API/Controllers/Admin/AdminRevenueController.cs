using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Admin
{
    // Backs Admin ▸ Revenue only (https://.../admin/revenue):
    //   - summary cards (total / candidate / recruiter / credits)
    //   - revenue-by-country panel + category composition
    //   - the filterable, paginated "Plan transactions" table
    //   - the invoice detail shown in the invoice modal
    [ApiController]
    [Route("api/admin/revenue")]
    [Authorize(Roles = "Admin")]
    public class AdminRevenueController : ControllerBase
    {
        private readonly IAdminRevenueService _service;

        public AdminRevenueController(IAdminRevenueService service)
        {
            _service = service;
        }

        // GET /api/admin/revenue/summary
        // Filters temporarily removed for QA testing of the plan → purchase
        // → membership → revenue flow. Always returns the all-time,
        // all-country figures so there's one number for the tester to
        // reconcile by hand. See IAdminRevenueService for how to restore them.
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var data = await _service.GetSummaryAsync();

            return Ok(new
            {
                success = true,
                message = "Revenue summary retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/by-country
        // Filters temporarily removed for QA testing (see GetSummary above).
        // Always returns the current calendar month, all countries.
        [HttpGet("by-country")]
        public async Task<IActionResult> GetByCountry()
        {
            var data = await _service.GetRevenueByCountryAsync();

            return Ok(new
            {
                success = true,
                message = "Revenue by country retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/transactions?page=1&pageSize=10
        // Filters (type/country/search/date range) temporarily removed for
        // QA testing (see GetSummary above). Pagination is kept since it's
        // not a data filter — without it the list could try to return every
        // row at once.
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _service.GetTransactionsAsync(page, pageSize);

            return Ok(new
            {
                success = true,
                message = "Revenue transactions retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/transactions/{transactionId}/invoice
        [HttpGet("transactions/{transactionId:guid}/invoice")]
        public async Task<IActionResult> GetTransactionInvoice(Guid transactionId)
        {
            var data = await _service.GetTransactionInvoiceAsync(transactionId);

            if (data == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Invoice retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/transactions/{transactionId}/invoice/download
        // Redirects to the invoice's stored S3 URL. Returns 404 when the
        // transaction has no invoice on file yet.
        [HttpGet("transactions/{transactionId:guid}/invoice/download")]
        public async Task<IActionResult> DownloadInvoice(Guid transactionId)
        {
            var data = await _service.GetTransactionInvoiceAsync(transactionId);

            if (data?.InvoiceUrl == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invoice file not found for this transaction."
                });
            }

            return Redirect(data.InvoiceUrl);
        }
    }
}