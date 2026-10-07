using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminCouponService
    {
        Task<CouponResponseDto> CreateCouponAsync(
            CreateCouponRequestDto request,
            Guid adminUserId);

        Task<CouponResponseDto> UpdateCouponAsync(
            Guid couponId,
            UpdateCouponRequestDto request,
            Guid adminUserId);

        Task<CouponResponseDto?> GetCouponByIdAsync(
            Guid couponId);

        Task<List<CouponResponseDto>> GetCouponsAsync(
            PlanType? planType = null,
            string? region = null,
            bool? isActive = null);

        Task<bool> DeactivateCouponAsync(
            Guid couponId,
            Guid adminUserId);
    }
}
