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
        /// List support tickets (candidates + recruiters), filterable by
        /// raiser type, status, category and free-text search, paginated.
        /// Backs the tabs, filter bar and table on /admin/helpAndsupport.
        /// </summary>
        // GET /api/admin/support-tickets?raisedByType=&status=&category=&search=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> GetTickets(
            [FromQuery] AdminSupportTicketListRequestDto request)
        {
            var result = await _supportTicketService.GetTicketsAsync(request);
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
        [HttpPost("{ticketId:guid}/reply")]
        public async Task<IActionResult> AddReply(
            Guid ticketId,
            [FromBody] AdminAddTicketReplyRequestDto request)
        {
            var adminId = User.GetAdminId();

            var result = await _supportTicketService.AddReplyAsync(
                ticketId,
                adminId,
                request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion
    }
}