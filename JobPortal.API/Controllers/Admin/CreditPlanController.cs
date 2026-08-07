using JobPortal.API.Controllers.Recruiter;
using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.Implement.Admin;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{


    [ApiController]
    [Route("api/admin/credit-plans")]
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
            [FromBody] CreateCreditPlanRequestDto request,
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result =
                await _service.CreatePlanAsync(
                    request,
                    adminId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPut]
        [AuditLog("Update Credit Plan", "Credit Plans", AuditSeverity.Warning)]
        public async Task<IActionResult> UpdatePlan(
            [FromBody] UpdateCreditPlanRequestDto request,
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result =
                await _service.UpdatePlanAsync(
                    request,
                    adminId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // Deliberately Critical (not defaulted by the heuristic): removing
        // a credit plan affects live pricing for employers.
        [HttpDelete("{planId}")]
        [AuditLog("Delete Credit Plan", "Credit Plans", AuditSeverity.Critical)]
        public async Task<IActionResult> DeletePlan(
            Guid planId,
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result =
                await _service.DeletePlanAsync(
                    planId,
                    adminId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans(
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result =
                await _service.GetAllPlansAsync(adminId);

            return Ok(result);
        }

        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(
            Guid planId,
            [FromHeader(Name = "AdminId")] Guid adminId)
        {
            var result =
                await _service.GetPlanByIdAsync(
                    planId,
                    adminId);

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