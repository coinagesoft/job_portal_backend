using JobPortal.Application.DTOs;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> UpdateAccountStatus(Guid id, [FromBody] UpdateAccountStatusRequestDto request)
        {
            try
            {
                var updated = await _adminCandidateService.UpdateAccountStatusAsync(id, request);
                if (!updated) return NotFound();
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
