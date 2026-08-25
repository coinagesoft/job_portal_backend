using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.SupportTicket;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/support-tickets")]
    [Authorize]
    public class AdminSupportTicketController : ControllerBase
    {
        private readonly IAdminSupportTicketService _supportTicketService;

        public AdminSupportTicketController(
            IAdminSupportTicketService supportTicketService)
        {
            _supportTicketService = supportTicketService;
        }

        #region List Tickets

        /// <summary>
        /// List support tickets (candidates + recruiters).
        /// Backs the tabs, filter bar and table on /admin/helpAndsupport.
        /// </summary>
        // GET /api/admin/support-tickets
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var result = await _supportTicketService.GetTicketsAsync(new AdminSupportTicketListRequestDto());
            return Ok(result);
        }

        #endregion

        #region Summary

        /// <summary>
        /// Tab counts (Candidates / Recruiters) and their status breakdown.
        /// </summary>
        // GET /api/admin/support-tickets/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _supportTicketService.GetSummaryAsync();
            return Ok(result);
        }

        #endregion

        #region Ticket Thread

        /// <summary>
        /// Full ticket detail + conversation thread. Backs the review
        /// drawer on /admin/helpAndsupport.
        /// </summary>
        // GET /api/admin/support-tickets/{ticketId}
        [HttpGet("{ticketId:guid}")]
        public async Task<IActionResult> GetTicketThread(Guid ticketId)
        {
            var result = await _supportTicketService.GetTicketThreadAsync(ticketId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Ticket not found."
                });
            }

            return Ok(result);
        }

        #endregion

        #region Reply

        /// <summary>
        /// Admin's ONLY action on a ticket — send a chat reply. There is
        /// intentionally no resolve/status endpoint on this controller:
        /// resolution belongs to the candidate/recruiter (their own
        /// "Resolve" action) or to the 48-hour auto-resolve background job
        /// (see SupportTicketAutoResolveService).
        /// </summary>
        // POST /api/admin/support-tickets/{ticketId}/reply
        // [SkipAuditLog]: this writes its own richer AuditLog entry
        // (Module "Help & Support") inside AdminSupportTicketService,
        // the same way Create/Update/Delete Sub Admin and Login do —
        // so the generic AuditLogMiddleware is skipped here to avoid a
        // duplicate, plainer entry.
        [HttpPost("{ticketId:guid}/reply")]
        [SkipAuditLog]
        public async Task<IActionResult> AddReply(
            Guid ticketId,
            [FromBody] AdminAddTicketReplyRequestDto request)
        {
            var adminId = User.GetAdminId();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers.UserAgent.ToString();
            var jwtId = User.FindFirst("jti")?.Value;

            var result = await _supportTicketService.AddReplyAsync(
                ticketId,
                adminId,
                request,
                ipAddress,
                userAgent,
                jwtId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}