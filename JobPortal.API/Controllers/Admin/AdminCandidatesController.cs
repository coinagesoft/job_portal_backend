using JobPortal.Application.DTOs;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.Implement.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/candidates")]
    [Authorize(Roles = "Admin")] // adjust to match your actual admin auth scheme
    public class AdminCandidatesController : ControllerBase
    {
        private readonly IAdminCandidateService _adminCandidateService;

        public AdminCandidatesController(IAdminCandidateService adminCandidateService)
        {
            _adminCandidateService = adminCandidateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCandidates()
        {
            var candidates = await _adminCandidateService.GetCandidatesAsync();
            return Ok(candidates);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCandidateDetail(Guid id)
        {
            var detail = await _adminCandidateService.GetCandidateDetailAsync(id);
            if (detail == null) return NotFound();
            return Ok(detail);
        }



        [HttpPatch("{id:guid}/account-status")]
        public async Task<IActionResult> UpdateAccountStatus(
          Guid id,
          [FromBody] UpdateAccountStatusRequestDto request)
        {
            var audit = new AdminAuditContext
            {
                AdminId = Guid.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)!.Value
                ),
                AdminName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown",
                AdminRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            try
            {
                var updated = await _adminCandidateService
                    .UpdateAccountStatusAsync(id, request, audit);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Candidate account not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Candidate account status updated successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}




    
