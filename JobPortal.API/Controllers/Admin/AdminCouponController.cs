using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/coupons")]
    [Authorize(Roles = "Admin")]
    public class AdminCouponController : ControllerBase
    {
        private readonly IAdminCouponService _service;
        private readonly ILogger<AdminCouponController> _logger;

        public AdminCouponController(
            IAdminCouponService service,
            ILogger<AdminCouponController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ============================================================
        // GET /api/admin/coupons
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetCoupons(
            [FromQuery] PlanType? planType = null,
            [FromQuery] string? region = null,
            [FromQuery] bool? isActive = null)
        {
            var result = await _service.GetCouponsAsync(
                planType,
                region,
                isActive);

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        // ============================================================
        // GET /api/admin/coupons/{couponId}
        // ============================================================

        [HttpGet("{couponId:guid}")]
        public async Task<IActionResult> GetCouponById(
            Guid couponId)
        {
            var result = await _service.GetCouponByIdAsync(
                couponId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Coupon not found."
                });
            }

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        // ============================================================
        // POST /api/admin/coupons
        // ============================================================

        [HttpPost]
        [AuditLog(
            "Create Coupon",
            "Coupons",
            AuditSeverity.Info)]
        public async Task<IActionResult> CreateCoupon(
            [FromBody] CreateCouponRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _service.CreateCouponAsync(
                    request,
                    User.GetAdminId());

                return Ok(new
                {
                    success = true,
                    message = "Coupon created successfully.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // PUT /api/admin/coupons/{couponId}
        // ============================================================

        [HttpPut("{couponId:guid}")]
        [AuditLog(
            "Update Coupon",
            "Coupons",
            AuditSeverity.Warning)]
        public async Task<IActionResult> UpdateCoupon(
            Guid couponId,
            [FromBody] UpdateCouponRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _service.UpdateCouponAsync(
                    couponId,
                    request,
                    User.GetAdminId());

                return Ok(new
                {
                    success = true,
                    message = "Coupon updated successfully.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // PATCH /api/admin/coupons/{couponId}/deactivate
        // ============================================================

        [HttpPatch("{couponId:guid}/deactivate")]
        [AuditLog(
            "Deactivate Coupon",
            "Coupons",
            AuditSeverity.Warning)]
        public async Task<IActionResult> DeactivateCoupon(
            Guid couponId)
        {
            try
            {
                var result = await _service.DeactivateCouponAsync(
                    couponId,
                    User.GetAdminId());

                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Coupon not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Coupon deactivated successfully."
                });
            }
            catch (InvalidOperationException ex)
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