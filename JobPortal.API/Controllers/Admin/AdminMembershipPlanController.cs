using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.MembershipPlan;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    // Admin CRUD for recruiter / candidate lifetime membership plans,
    // priced per pricing region. Backs the "Recruiter membership" and
    // "Candidate membership" tabs on the admin Plans page.
    [ApiController]
    [Route("api/admin/membership-plans")]
    [Authorize]
    public class AdminMembershipPlanController : ControllerBase
    {
        private readonly IMembershipPlanService _service;
        private readonly ILogger<AdminMembershipPlanController> _logger;

        public AdminMembershipPlanController(
            IMembershipPlanService service,
            ILogger<AdminMembershipPlanController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ── GET /api/admin/membership-plans?planType=&region= ────────
        [HttpGet]
        public async Task<IActionResult> GetAllPlans(
            [FromQuery] PlanType? planType = null,
            [FromQuery] string? region = null)
        {
            var result = await _service.GetAllPlansAsync(planType, region);
            return Ok(result);
        }

        // ── GET /api/admin/membership-plans/{planId} ──────────────────
        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(Guid planId)
        {
            var result = await _service.GetPlanByIdAsync(planId);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Membership plan not found."
                });
            }

            return Ok(result);
        }

        // ── POST /api/admin/membership-plans ──────────────────────────
        [HttpPost]
        [AuditLog("Create Membership Plan", "Membership Plans", AuditSeverity.Info)]
        public async Task<IActionResult> CreatePlan(
            [FromBody] CreateMembershipPlanRequestDto request)
        {
            var result = await _service.CreatePlanAsync(request, User.GetAdminId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── PUT /api/admin/membership-plans ────────────────────────────
        [HttpPut]
        [AuditLog("Update Membership Plan", "Membership Plans", AuditSeverity.Warning)]
        public async Task<IActionResult> UpdatePlan(
            [FromBody] UpdateMembershipPlanRequestDto request)
        {
            var result = await _service.UpdatePlanAsync(request, User.GetAdminId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Deliberately Critical: removing a membership plan affects
        // live pricing shown to recruiters/candidates.
        // ── DELETE /api/admin/membership-plans/{planId} ────────────────
        [HttpDelete("{planId}")]
        [AuditLog("Delete Membership Plan", "Membership Plans", AuditSeverity.Critical)]
        public async Task<IActionResult> DeletePlan(
            Guid planId)
        {
            var result = await _service.DeletePlanAsync(planId, User.GetAdminId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}