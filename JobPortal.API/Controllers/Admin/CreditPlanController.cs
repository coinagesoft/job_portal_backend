using JobPortal.API.Controllers.Recruiter;
using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.Implement.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{


    [ApiController]
    [Route("api/admin/credit-plans")]
    [Authorize(Roles = "Admin")]
    public class AdminCreditPlanController : ControllerBase
    {
        private readonly ICreditPlanService _service;
        private readonly ILogger<CreditPlanService> _logger;
        public AdminCreditPlanController(
            ILogger<CreditPlanService> logger,
            ICreditPlanService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost]
        [AuditLog("Create Credit Plan", "Credit Plans", AuditSeverity.Info)]
        public async Task<IActionResult> CreatePlan(
            [FromBody] CreateCreditPlanRequestDto request)
        {
            var result =
                await _service.CreatePlanAsync(
                    request,
                    User.GetAdminId());

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPut]
        [AuditLog("Update Credit Plan", "Credit Plans", AuditSeverity.Warning)]
        public async Task<IActionResult> UpdatePlan(
            [FromBody] UpdateCreditPlanRequestDto request)
        {
            var result =
                await _service.UpdatePlanAsync(
                    request,
                    User.GetAdminId());

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // Deliberately Critical (not defaulted by the heuristic): removing
        // a credit plan affects live pricing for employers.
        [HttpDelete("{planId}")]
        [AuditLog("Delete Credit Plan", "Credit Plans", AuditSeverity.Critical)]
        public async Task<IActionResult> DeletePlan(
            Guid planId)
        {
            var result =
                await _service.DeletePlanAsync(
                    planId,
                    User.GetAdminId());

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans(
            [FromQuery] string? region = null)
        {
            var result =
                await _service.GetAllPlansAsync(User.GetAdminId(), region);

            return Ok(result);
        }

        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(
            Guid planId)
        {
            var result =
                await _service.GetPlanByIdAsync(
                    planId,
                    User.GetAdminId());

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Credit plan not found."
                });
            }

            return Ok(result);
        }

    }
}