using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Admin
{
    // Backs Admin ▸ Dashboard only (https://.../admin/dashboard) — the
    // platform-wide summary page. Every section on the page has its own
    // GET endpoint so the frontend can fetch/refresh sections
    // independently instead of one large combined payload:
    //   - Stats widgets
    //   - Registration Growth
    //   - Recruiters by Industry
    //   - Revenue & Credit Growth
    //   - Platform Overview
    //   - Recent Registrations
    //   - Recent Support Tickets
    //   - Recent Payments
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _service;

        public AdminDashboardController(IAdminDashboardService service)
        {
            _service = service;
        }

        // GET /api/admin/dashboard/stats-widgets
        [HttpGet("stats-widgets")]
        public async Task<IActionResult> GetStatsWidgets()
        {
            var data = await _service.GetStatsWidgetsAsync();

            return Ok(new
            {
                success = true,
                message = "Dashboard stats widgets retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/registration-growth?range=week|month|year
        [HttpGet("registration-growth")]
        public async Task<IActionResult> GetRegistrationGrowth([FromQuery] string range = "week")
        {
            var data = await _service.GetRegistrationGrowthAsync(range);

            return Ok(new
            {
                success = true,
                message = "Registration growth retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/recruiters-by-industry
        [HttpGet("recruiters-by-industry")]
        public async Task<IActionResult> GetRecruitersByIndustry()
        {
            var data = await _service.GetRecruitersByIndustryAsync();

            return Ok(new
            {
                success = true,
                message = "Recruiters by industry retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/revenue-credit-growth?months=6
        [HttpGet("revenue-credit-growth")]
        public async Task<IActionResult> GetRevenueCreditGrowth([FromQuery] int months = 6)
        {
            var data = await _service.GetRevenueCreditGrowthAsync(months);

            return Ok(new
            {
                success = true,
                message = "Revenue and credit growth retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/platform-overview
        [HttpGet("platform-overview")]
        public async Task<IActionResult> GetPlatformOverview()
        {
            var data = await _service.GetPlatformOverviewAsync();

            return Ok(new
            {
                success = true,
                message = "Platform overview retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/recent-registrations?limit=5
        [HttpGet("recent-registrations")]
        public async Task<IActionResult> GetRecentRegistrations([FromQuery] int limit = 5)
        {
            var data = await _service.GetRecentRegistrationsAsync(limit);

            return Ok(new
            {
                success = true,
                message = "Recent registrations retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/recent-support-tickets?limit=5
        [HttpGet("recent-support-tickets")]
        public async Task<IActionResult> GetRecentSupportTickets([FromQuery] int limit = 5)
        {
            var data = await _service.GetRecentSupportTicketsAsync(limit);

            return Ok(new
            {
                success = true,
                message = "Recent support tickets retrieved successfully.",
                data
            });
        }

        // GET /api/admin/dashboard/recent-payments?limit=5
        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments([FromQuery] int limit = 5)
        {
            var data = await _service.GetRecentPaymentsAsync(limit);

            return Ok(new
            {
                success = true,
                message = "Recent payments retrieved successfully.",
                data
            });
        }
    }
}