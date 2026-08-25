using JobPortal.API.Controllers.Recruiter;
using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter;
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
        private readonly ICreditConfigurationService _configService;
        private readonly ILogger<CreditPlanService> _logger;
        public AdminCreditPlanController(
            ILogger<CreditPlanService> logger,
            ICreditPlanService service,
            ICreditConfigurationService configService)
        {
            _logger = logger;
            _service = service;
            _configService = configService;
        }

        // ── Credit configuration (profile unlock / CV download costs) ──
        // Global, single-row settings that control how many credits are
        // deducted for a profile unlock (or CV download) anywhere in the
        // app — e.g. CreditWalletService.UnlockCandidateAsync reads
        // ProfileUnlockCredits from here instead of a hardcoded number.
        // This is intentionally separate from the CreditPlan CRUD above
        // (those are purchasable packages; this is the deduction rate).

        [HttpGet("configuration")]
        [ProducesResponseType(typeof(CreditConfigurationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetConfiguration()
        {
            var result = await _configService.GetConfigurationAsync();

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Credit configuration not found."
                });
            }

            return Ok(result);
        }

        [HttpPut("configuration")]
        [AuditLog("Update Credit Configuration", "Credit Plans", AuditSeverity.Warning)]
        public async Task<IActionResult> UpdateConfiguration(
            [FromBody] UpdateCreditConfigurationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _configService.UpdateConfigurationAsync(
                request,
                User.GetAdminId());

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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