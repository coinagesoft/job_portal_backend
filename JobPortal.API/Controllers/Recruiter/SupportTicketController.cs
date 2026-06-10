using JobPortal.Application.DTOs.Recruiter.SupportTicket;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/support-tickets")]
    public class SupportTicketController : ControllerBase
    {
        private readonly ISupportTicketService _supportTicketService;

        public SupportTicketController(
            ISupportTicketService supportTicketService)
        {
            _supportTicketService = supportTicketService;
        }

        /// <summary>
        /// Create Support Ticket
        /// </summary>
        [HttpPost("{employerId:guid}")]
        public async Task<IActionResult> CreateTicket(
            Guid employerId,
            [FromForm] CreateSupportTicketRequestDto request)
        {
            var result = await _supportTicketService
                .CreateTicketAsync(
                    employerId,
                    request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get All Tickets
        /// </summary>
        [HttpGet("{employerId:guid}")]
        public async Task<IActionResult> GetTickets(
            Guid employerId)
        {
            var result = await _supportTicketService
                .GetTicketsAsync(employerId);

            return Ok(result);
        }

        /// <summary>
        /// Get Ticket Thread
        /// </summary>
        [HttpGet("thread/{ticketId:guid}")]
        public async Task<IActionResult> GetTicketThread(
            Guid ticketId)
        {
            var result = await _supportTicketService
                .GetTicketThreadAsync(ticketId);

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

        /// <summary>
        /// Add Reply To Ticket
        /// </summary>
        [HttpPost("{ticketId:guid}/reply/{employerId:guid}")]
        public async Task<IActionResult> AddReply(
      Guid ticketId,
      Guid employerId,
      [FromBody] AddTicketReplyRequestDto request)
        {
            var result = await _supportTicketService.AddReplyAsync(
                ticketId,
                employerId,
                request);

            return Ok(result);
        }

        /// <summary>
        /// Mark Ticket Resolved
        /// </summary>
        [HttpPatch("{ticketId:guid}/resolve")]
        public async Task<IActionResult> ResolveTicket(
            Guid ticketId)
        {
            var result = await _supportTicketService
                .ResolveTicketAsync(ticketId);

            if (!result)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Ticket not found."
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Ticket resolved successfully."
            });
        }

        /// <summary>
        /// Ticket Summary
        /// </summary>
        [HttpGet("{employerId:guid}/summary")]
        public async Task<IActionResult> GetSummary(
            Guid employerId)
        {
            var result = await _supportTicketService
                .GetSummaryAsync(employerId);

            return Ok(result);
        }
    }
}