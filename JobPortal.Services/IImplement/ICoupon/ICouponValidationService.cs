using JobPortal.Application.DTOs.Admin.Coupons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.ICoupon
{
    public interface ICouponValidationService
    {
        Task<CouponValidationResponseDto> ValidateCouponAsync(
            ValidateCouponRequestDto request,
            Guid userId);
    }
}
