using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{


    [ApiController]
    [Route("api/admin/credit-plans")]
    public class CreditPlanController : ControllerBase
    {
        private readonly ICreditPlanService _service;

        public CreditPlanController(
            ICreditPlanService service)
        {
            _service = service;
        }

        [HttpPost]
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

        [HttpDelete("{planId}")]
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
