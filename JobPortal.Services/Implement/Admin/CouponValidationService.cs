using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICoupon;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Coupons
{
    public class CouponValidationService : ICouponValidationService
    {
        private readonly AppDbContext _db;

        public CouponValidationService(AppDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // VALIDATE COUPON
        // Used by Candidate + Recruiter membership purchase flows
        // ============================================================

        public async Task<CouponValidationResponseDto> ValidateCouponAsync(
            ValidateCouponRequestDto request,
            Guid userId)
        {
            var response = new CouponValidationResponseDto
            {
                IsValid = false,
                PlanId = request.PlanId
            };

            // --------------------------------------------------------
            // Basic request validation
            // --------------------------------------------------------

            if (request == null)
            {
                response.Message = "Invalid coupon request.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                response.Message = "Coupon code is required.";
                return response;
            }

            if (request.PlanId == Guid.Empty)
            {
                response.Message = "Membership plan is required.";
                return response;
            }

            if (userId == Guid.Empty)
            {
                // Candidate registration can validate a coupon before
                // the User record exists. Per-user usage is checked
                // again during final registration after the UserId is created.
            }

            var code = NormalizeCode(request.Code);
            var region = NormalizeRegion(request.Region);

            // --------------------------------------------------------
            // Get exact membership plan
            // --------------------------------------------------------
            //
            // IMPORTANT:
            // We do NOT accept price from frontend.
            // Price always comes from MembershipPlan.
            //
            // --------------------------------------------------------

            var plan = await _db.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PlanId == request.PlanId &&
                    x.IsActive);

            if (plan == null)
            {
                response.Message =
                    "Membership plan not found or inactive.";

                return response;
            }

            // --------------------------------------------------------
            // Verify PlanType
            // --------------------------------------------------------

            if (plan.PlanType != request.PlanType)
            {
                response.Message =
                    "The selected plan does not match the membership type.";

                return response;
            }

            // --------------------------------------------------------
            // Verify region
            // --------------------------------------------------------

            if (!string.Equals(
                    plan.Region,
                    region,
                    StringComparison.OrdinalIgnoreCase))
            {
                response.Message =
                    "The selected plan does not belong to the selected region.";

                return response;
            }

            // --------------------------------------------------------
            // Find coupon
            // --------------------------------------------------------

            var coupon = await _db.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == code);

            if (coupon == null)
            {
                response.Message = "Invalid coupon code.";
                return response;
            }

            // --------------------------------------------------------
            // Active check
            // --------------------------------------------------------

            if (!coupon.IsActive)
            {
                response.Message =
                    "This coupon is inactive.";

                return response;
            }

            // --------------------------------------------------------
            // Start date
            // --------------------------------------------------------

            var now = DateTime.UtcNow;

            if (coupon.StartAt.HasValue &&
                coupon.StartAt.Value > now)
            {
                response.Message =
                    "This coupon is not active yet.";

                return response;
            }

            // --------------------------------------------------------
            // Expiry
            // --------------------------------------------------------

            if (coupon.ExpiresAt.HasValue &&
                coupon.ExpiresAt.Value < now)
            {
                response.Message =
                    "This coupon has expired.";

                return response;
            }

            // --------------------------------------------------------
            // PlanType check
            // --------------------------------------------------------

            if (coupon.PlanType != plan.PlanType)
            {
                response.Message =
                    "This coupon is not valid for this membership type.";

                return response;
            }

            // --------------------------------------------------------
            // Region check
            // --------------------------------------------------------

            if (!string.Equals(
                    coupon.Region,
                    plan.Region,
                    StringComparison.OrdinalIgnoreCase))
            {
                response.Message =
                    "This coupon is not valid for this region.";

                return response;
            }

            // --------------------------------------------------------
            // Plan check
            // --------------------------------------------------------
            //
            // PlanId = null means:
            //
            // All plans of this PlanType + Region
            //
            // PlanId != null means:
            //
            // Only that exact plan
            //
            // --------------------------------------------------------

            if (coupon.PlanId.HasValue &&
                coupon.PlanId.Value != plan.PlanId)
            {
                response.Message =
                    "This coupon is not valid for the selected plan.";

                return response;
            }

            // --------------------------------------------------------
            // Global usage limit
            // --------------------------------------------------------

            if (coupon.UsageLimit.HasValue &&
                coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                response.Message =
                    "This coupon has reached its usage limit.";

                return response;
            }

            // --------------------------------------------------------
            // Per-user usage limit
            // --------------------------------------------------------
            if (coupon.PerUserLimit.HasValue && userId != Guid.Empty)
            {
                var userUsageCount = await _db.CouponRedemptions
                    .AsNoTracking()
                    .CountAsync(x =>
                        x.CouponId == coupon.CouponId &&
                        x.UserId == userId);

                if (userUsageCount >= coupon.PerUserLimit.Value)
                {
                    response.Message =
                        "You have already used this coupon the maximum allowed number of times.";

                    return response;
                }
            }

            // --------------------------------------------------------
            // Get original plan amount
            // --------------------------------------------------------

            var originalAmount = plan.Price;

            if (originalAmount < 0)
            {
                response.Message =
                    "Invalid membership plan price.";

                return response;
            }

            // --------------------------------------------------------
            // Minimum purchase amount
            // --------------------------------------------------------

            if (coupon.MinimumAmount.HasValue &&
                originalAmount < coupon.MinimumAmount.Value)
            {
                response.Message =
                    $"Minimum purchase amount for this coupon is {coupon.MinimumAmount.Value:N2}.";

                return response;
            }

            // --------------------------------------------------------
            // Calculate discount
            // --------------------------------------------------------

            decimal discountAmount = 0;

            decimal? discountPercentage = null;

            if (coupon.DiscountType == CouponDiscountType.Percentage)
            {
                // ----------------------------------------------------
                // Percentage discount
                // ----------------------------------------------------

                if (coupon.DiscountValue <= 0 ||
                    coupon.DiscountValue > 100)
                {
                    response.Message =
                        "Invalid percentage discount configured for this coupon.";

                    return response;
                }

                discountPercentage = coupon.DiscountValue;

                discountAmount =
                    originalAmount *
                    coupon.DiscountValue /
                    100m;
            }
            else if (coupon.DiscountType == CouponDiscountType.Fixed)
            {
                // ----------------------------------------------------
                // Fixed discount
                // ----------------------------------------------------

                if (coupon.DiscountValue <= 0)
                {
                    response.Message =
                        "Invalid fixed discount configured for this coupon.";

                    return response;
                }

                discountAmount =
                    coupon.DiscountValue;
            }
            else
            {
                response.Message =
                    "Invalid coupon discount type.";

                return response;
            }

            // --------------------------------------------------------
            // Maximum discount
            // --------------------------------------------------------

            if (coupon.MaximumDiscount.HasValue &&
                discountAmount > coupon.MaximumDiscount.Value)
            {
                discountAmount =
                    coupon.MaximumDiscount.Value;
            }

            // --------------------------------------------------------
            // Never allow discount greater than plan price
            // --------------------------------------------------------

            if (discountAmount > originalAmount)
            {
                discountAmount = originalAmount;
            }

            // --------------------------------------------------------
            // Never allow negative discount
            // --------------------------------------------------------

            if (discountAmount < 0)
            {
                discountAmount = 0;
            }

            // --------------------------------------------------------
            // Calculate final amount
            // --------------------------------------------------------

            var finalAmount =
                originalAmount - discountAmount;

            if (finalAmount < 0)
            {
                finalAmount = 0;
            }

            // --------------------------------------------------------
            // Return successful validation
            // --------------------------------------------------------

            response.IsValid = true;

            response.Message =
                "Coupon applied successfully.";

            response.CouponId =
                coupon.CouponId;

            response.CouponCode =
                coupon.Code;

            response.PlanId =
                plan.PlanId;

            response.OriginalAmount =
                originalAmount;

            response.DiscountAmount =
                discountAmount;

            response.FinalAmount =
                finalAmount;

            response.Currency =
                "INR";

            response.DiscountPercentage =
                discountPercentage;

            return response;
        }

        // ============================================================
        // NORMALIZE CODE
        // ============================================================

        private static string NormalizeCode(string code)
        {
            return code
                .Trim()
                .ToUpperInvariant();
        }

        // ============================================================
        // NORMALIZE REGION
        // ============================================================

        private static string NormalizeRegion(string region)
        {
            return region
                .Trim()
                .ToLowerInvariant();
        }
    }
}
