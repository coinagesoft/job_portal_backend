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

        // GET /api/admin/revenue/summary?country=India&dateFrom=2026-07-01&dateTo=2026-07-31
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string? country,
            [FromQuery] DateOnly? dateFrom,
            [FromQuery] DateOnly? dateTo)
        {
            var data = await _service.GetSummaryAsync(country, dateFrom, dateTo);

            return Ok(new
            {
                success = true,
                message = "Revenue summary retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/by-country?period=monthly&country=India
        [HttpGet("by-country")]
        public async Task<IActionResult> GetByCountry(
            [FromQuery] string period = "monthly",
            [FromQuery] string? country = null)
        {
            var data = await _service.GetRevenueByCountryAsync(period, country);

            return Ok(new
            {
                success = true,
                message = "Revenue by country retrieved successfully.",
                data
            });
        }

        // GET /api/admin/revenue/transactions?type=all&country=&search=&dateFrom=&dateTo=&page=1&pageSize=10
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string type = "all",
            [FromQuery] string? country = null,
            [FromQuery] string? search = null,
            [FromQuery] DateOnly? dateFrom = null,
            [FromQuery] DateOnly? dateTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var data = await _service.GetTransactionsAsync(
                type, country, search, dateFrom, dateTo, page, pageSize);

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