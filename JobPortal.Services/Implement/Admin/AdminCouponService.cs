using JobPortal.Application.DTOs.Admin.Coupons;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Admin
{
    public class AdminCouponService : IAdminCouponService
    {
        private readonly AppDbContext _db;

        public AdminCouponService(AppDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // CREATE COUPON
        // ============================================================

        public async Task<CouponResponseDto> CreateCouponAsync(
            CreateCouponRequestDto request,
            Guid adminUserId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var code = NormalizeCode(request.Code);
            var region = NormalizeRegion(request.Region);

            // --------------------------------------------------------
            // Validate coupon code
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException(
                    "Coupon code is required.");
            }

            // --------------------------------------------------------
            // Check duplicate coupon code
            // --------------------------------------------------------

            var existingCoupon = await _db.Coupons
                .AsNoTracking()
                .AnyAsync(x => x.Code == code);

            if (existingCoupon)
            {
                throw new InvalidOperationException(
                    "A coupon with this code already exists.");
            }

            // --------------------------------------------------------
            // Validate dates
            // --------------------------------------------------------

            ValidateDates(
                request.StartAt,
                request.ExpiresAt);

            // --------------------------------------------------------
            // Validate discount
            // --------------------------------------------------------

            ValidateDiscount(
                request.DiscountType,
                request.DiscountValue);

            // --------------------------------------------------------
            // Validate numeric values
            // --------------------------------------------------------

            ValidateAmounts(
                request.MinimumAmount,
                request.MaximumDiscount);

            ValidateUsageLimits(
                request.UsageLimit,
                request.PerUserLimit);

            // --------------------------------------------------------
            // Validate selected plan
            //
            // If PlanId is null:
            // coupon applies to all plans of PlanType + Region
            // --------------------------------------------------------

            if (request.PlanId.HasValue)
            {
                await ValidatePlanAsync(
                    request.PlanId.Value,
                    request.PlanType,
                    region);
            }

            // --------------------------------------------------------
            // Create entity
            // --------------------------------------------------------

            var coupon = new Coupon
            {
                CouponId = Guid.NewGuid(),

                Code = code,

                PlanType = request.PlanType,

                Region = region,

                PlanId = request.PlanId,

                DiscountType = request.DiscountType,

                DiscountValue = request.DiscountValue,

                MinimumAmount = request.MinimumAmount,

                MaximumDiscount = request.MaximumDiscount,

                UsageLimit = request.UsageLimit,

                PerUserLimit = request.PerUserLimit,

                StartAt = request.StartAt,

                ExpiresAt = request.ExpiresAt,

                IsActive = request.IsActive,

                UsedCount = 0,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = adminUserId
            };

            _db.Coupons.Add(coupon);

            await _db.SaveChangesAsync();

            return await GetCouponByIdAsync(coupon.CouponId)
                   ?? throw new InvalidOperationException(
                       "Coupon was created but could not be retrieved.");
        }

        // ============================================================
        // UPDATE COUPON
        // ============================================================

        public async Task<CouponResponseDto> UpdateCouponAsync(
            Guid couponId,
            UpdateCouponRequestDto request,
            Guid adminUserId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(x =>
                    x.CouponId == couponId);

            if (coupon == null)
            {
                throw new KeyNotFoundException(
                    "Coupon not found.");
            }

            var code = NormalizeCode(request.Code);
            var region = NormalizeRegion(request.Region);

            // --------------------------------------------------------
            // Check duplicate code
            // --------------------------------------------------------

            var duplicateCode = await _db.Coupons
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CouponId != couponId &&
                    x.Code == code);

            if (duplicateCode)
            {
                throw new InvalidOperationException(
                    "Another coupon with this code already exists.");
            }

            // --------------------------------------------------------
            // Validate dates
            // --------------------------------------------------------

            ValidateDates(
                request.StartAt,
                request.ExpiresAt);

            // --------------------------------------------------------
            // Validate discount
            // --------------------------------------------------------

            ValidateDiscount(
                request.DiscountType,
                request.DiscountValue);

            // --------------------------------------------------------
            // Validate amounts
            // --------------------------------------------------------

            ValidateAmounts(
                request.MinimumAmount,
                request.MaximumDiscount);

            // --------------------------------------------------------
            // Validate usage limits
            // --------------------------------------------------------

            ValidateUsageLimits(
                request.UsageLimit,
                request.PerUserLimit);

            // --------------------------------------------------------
            // Never allow usage limit below current usage
            // --------------------------------------------------------

            if (request.UsageLimit.HasValue &&
                request.UsageLimit.Value < coupon.UsedCount)
            {
                throw new InvalidOperationException(
                    "Usage limit cannot be lower than the number of times this coupon has already been used.");
            }

            // --------------------------------------------------------
            // Validate selected plan
            // --------------------------------------------------------

            if (request.PlanId.HasValue)
            {
                await ValidatePlanAsync(
                    request.PlanId.Value,
                    request.PlanType,
                    region);
            }

            // --------------------------------------------------------
            // Update entity
            // --------------------------------------------------------

            coupon.Code = code;

            coupon.PlanType = request.PlanType;

            coupon.Region = region;

            coupon.PlanId = request.PlanId;

            coupon.DiscountType = request.DiscountType;

            coupon.DiscountValue = request.DiscountValue;

            coupon.MinimumAmount = request.MinimumAmount;

            coupon.MaximumDiscount = request.MaximumDiscount;

            coupon.UsageLimit = request.UsageLimit;

            coupon.PerUserLimit = request.PerUserLimit;

            coupon.StartAt = request.StartAt;

            coupon.ExpiresAt = request.ExpiresAt;

            coupon.IsActive = request.IsActive;

            coupon.UpdatedAt = DateTime.UtcNow;

            coupon.UpdatedBy = adminUserId;

            await _db.SaveChangesAsync();

            return await GetCouponByIdAsync(couponId)
                   ?? throw new InvalidOperationException(
                       "Coupon was updated but could not be retrieved.");
        }

        // ============================================================
        // GET COUPON BY ID
        // ============================================================

        public async Task<CouponResponseDto?> GetCouponByIdAsync(
            Guid couponId)
        {
            var coupon = await _db.Coupons
                .AsNoTracking()
                .Include(x => x.MembershipPlan)
                .FirstOrDefaultAsync(x =>
                    x.CouponId == couponId);

            if (coupon == null)
            {
                return null;
            }

            return MapCoupon(coupon);
        }

        // ============================================================
        // GET COUPONS
        // ============================================================

        public async Task<List<CouponResponseDto>> GetCouponsAsync(
            PlanType? planType = null,
            string? region = null,
            bool? isActive = null)
        {
            var query = _db.Coupons
                .AsNoTracking()
                .Include(x => x.MembershipPlan)
                .AsQueryable();

            // --------------------------------------------------------
            // Plan type filter
            // --------------------------------------------------------

            if (planType.HasValue)
            {
                query = query.Where(x =>
                    x.PlanType == planType.Value);
            }

            // --------------------------------------------------------
            // Region filter
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(region))
            {
                var normalizedRegion =
                    NormalizeRegion(region);

                query = query.Where(x =>
                    x.Region == normalizedRegion);
            }

            // --------------------------------------------------------
            // Active filter
            // --------------------------------------------------------

            if (isActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive == isActive.Value);
            }

            var coupons = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return coupons
                .Select(MapCoupon)
                .ToList();
        }

        // ============================================================
        // DEACTIVATE COUPON
        // ============================================================

        public async Task<bool> DeactivateCouponAsync(
            Guid couponId,
            Guid adminUserId)
        {
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(x =>
                    x.CouponId == couponId);

            if (coupon == null)
            {
                return false;
            }

            // --------------------------------------------------------
            // Already inactive
            // --------------------------------------------------------

            if (!coupon.IsActive)
            {
                return true;
            }

            coupon.IsActive = false;

            coupon.UpdatedAt = DateTime.UtcNow;

            coupon.UpdatedBy = adminUserId;

            await _db.SaveChangesAsync();

            return true;
        }

        // ============================================================
        // VALIDATE MEMBERSHIP PLAN
        // ============================================================

        private async Task ValidatePlanAsync(
            Guid planId,
            PlanType planType,
            string region)
        {
            var plan = await _db.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PlanId == planId);

            if (plan == null)
            {
                throw new InvalidOperationException(
                    "Selected membership plan was not found.");
            }

            if (!plan.IsActive)
            {
                throw new InvalidOperationException(
                    "Selected membership plan is inactive.");
            }

            if (plan.PlanType != planType)
            {
                throw new InvalidOperationException(
                    "Selected membership plan does not match the coupon PlanType.");
            }

            if (!string.Equals(
                    plan.Region,
                    region,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Selected membership plan does not match the coupon region.");
            }
        }

        // ============================================================
        // VALIDATE DATES
        // ============================================================

        private static void ValidateDates(
            DateTime? startAt,
            DateTime? expiresAt)
        {
            if (startAt.HasValue &&
                expiresAt.HasValue &&
                startAt.Value >= expiresAt.Value)
            {
                throw new InvalidOperationException(
                    "Start date must be earlier than expiry date.");
            }
        }

        // ============================================================
        // VALIDATE DISCOUNT
        // ============================================================

        private static void ValidateDiscount(
            CouponDiscountType discountType,
            decimal discountValue)
        {
            if (discountValue <= 0)
            {
                throw new InvalidOperationException(
                    "Discount value must be greater than zero.");
            }

            if (discountType == CouponDiscountType.Percentage &&
                discountValue > 100)
            {
                throw new InvalidOperationException(
                    "Percentage discount cannot exceed 100%.");
            }

            if (discountType != CouponDiscountType.Percentage &&
                discountType != CouponDiscountType.Fixed)
            {
                throw new InvalidOperationException(
                    "Invalid coupon discount type.");
            }
        }

        // ============================================================
        // VALIDATE AMOUNTS
        // ============================================================

        private static void ValidateAmounts(
            decimal? minimumAmount,
            decimal? maximumDiscount)
        {
            if (minimumAmount.HasValue &&
                minimumAmount.Value < 0)
            {
                throw new InvalidOperationException(
                    "Minimum amount cannot be negative.");
            }

            if (maximumDiscount.HasValue &&
                maximumDiscount.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Maximum discount must be greater than zero.");
            }
        }

        // ============================================================
        // VALIDATE USAGE LIMITS
        // ============================================================

        private static void ValidateUsageLimits(
            int? usageLimit,
            int? perUserLimit)
        {
            if (usageLimit.HasValue &&
                usageLimit.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Usage limit must be greater than zero.");
            }

            if (perUserLimit.HasValue &&
                perUserLimit.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Per-user usage limit must be greater than zero.");
            }

            if (usageLimit.HasValue &&
                perUserLimit.HasValue &&
                perUserLimit.Value > usageLimit.Value)
            {
                throw new InvalidOperationException(
                    "Per-user usage limit cannot be greater than the total usage limit.");
            }
        }

        // ============================================================
        // NORMALIZE COUPON CODE
        // ============================================================

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        // ============================================================
        // NORMALIZE REGION
        // ============================================================

        private static string NormalizeRegion(string? region)
        {
            return (region ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }

        // ============================================================
        // MAP ENTITY → DTO
        // ============================================================

        private static CouponResponseDto MapCoupon(
            Coupon coupon)
        {
            return new CouponResponseDto
            {
                CouponId = coupon.CouponId,

                Code = coupon.Code,

                PlanType = coupon.PlanType,

                Region = coupon.Region,

                PlanId = coupon.PlanId,

                PlanName = coupon.MembershipPlan?.PlanName,

                DiscountType = coupon.DiscountType,

                DiscountValue = coupon.DiscountValue,

                MinimumAmount = coupon.MinimumAmount,

                MaximumDiscount = coupon.MaximumDiscount,

                UsageLimit = coupon.UsageLimit,

                PerUserLimit = coupon.PerUserLimit,

                UsedCount = coupon.UsedCount,

                StartAt = coupon.StartAt,

                ExpiresAt = coupon.ExpiresAt,

                IsActive = coupon.IsActive,

                CreatedAt = coupon.CreatedAt,

                UpdatedAt = coupon.UpdatedAt
            };
        }
    }
}