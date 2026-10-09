using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Admin.MembershipPlan;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    public class MembershipPlanService : IMembershipPlanService
    {
        private readonly AppDbContext _context;

        public MembershipPlanService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommonResponseDto> CreatePlanAsync(
            CreateMembershipPlanRequestDto request,
            Guid adminId)
        {
            var plan = new MembershipPlan
            {
                PlanId = Guid.NewGuid(),
                PlanType = request.PlanType,
                Region = string.IsNullOrWhiteSpace(request.Region) ? "us" : request.Region,
                PlanName = request.PlanName,
                Description = request.Description,
                Price = request.Price,
                Period = string.IsNullOrWhiteSpace(request.Period) ? "one-time" : request.Period,
                Badge = request.Badge,
                Features = request.Features ?? new List<string>(),
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Membership plan created successfully.",
                PlanId = plan.PlanId
            };
        }

        public async Task<CommonResponseDto> UpdatePlanAsync(
            UpdateMembershipPlanRequestDto request,
            Guid adminId)
        {
            var plan = await _context.MembershipPlans
                .FirstOrDefaultAsync(x => x.PlanId == request.PlanId);

            if (plan == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Plan not found."
                };
            }

            plan.PlanType = request.PlanType;
            plan.Region = string.IsNullOrWhiteSpace(request.Region) ? plan.Region : request.Region;
            plan.PlanName = request.PlanName;
            plan.Description = request.Description;
            plan.Price = request.Price;
            plan.Period = string.IsNullOrWhiteSpace(request.Period) ? plan.Period : request.Period;
            plan.Badge = request.Badge;
            plan.Features = request.Features ?? new List<string>();
            plan.IsActive = request.IsActive;
            plan.UpdatedBy = adminId;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Membership plan updated successfully.",
                PlanId = plan.PlanId
            };
        }

        public async Task<CommonResponseDto> DeletePlanAsync(Guid planId, Guid adminId)
        {
            var plan = await _context.MembershipPlans
                .FirstOrDefaultAsync(x => x.PlanId == planId);

            if (plan == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Plan not found."
                };
            }

            plan.IsActive = false;
            plan.UpdatedBy = adminId;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Plan deactivated."
            };
        }

        public async Task<List<MembershipPlanResponseDto>> GetAllPlansAsync(
        PlanType? planType = null,
        string? region = null)
        {
            var query = _context.MembershipPlans.AsQueryable();

            if (planType.HasValue)
                query = query.Where(x => x.PlanType == planType.Value);

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(x => x.Region == region);

            var plans = await query
                .Include(x => x.Coupons)
                .OrderBy(x => x.Price)
                .ToListAsync();

            var allActiveCoupons = await _context.Coupons
                .Where(c => c.IsActive)
                .ToListAsync();

            return plans
                .Select(plan => ToResponseDto(plan, allActiveCoupons))
                .ToList();
        }

        public async Task<MembershipPlanResponseDto?> GetPlanByIdAsync(Guid planId)
        {
            var plan = await _context.MembershipPlans
                .AsNoTracking()
                .Include(x => x.Coupons)
                .FirstOrDefaultAsync(x => x.PlanId == planId);

            if (plan == null)
                return null;

            var allActiveCoupons = await _context.Coupons
                .Where(c => c.IsActive)
                .ToListAsync();

            return ToResponseDto(plan, allActiveCoupons);
        }

        public async Task<List<MembershipPlanResponseDto>> GetActivePlansAsync(
       PlanType planType,
       string? region = null)
        {
            var query = _context.MembershipPlans
                .Where(x => x.PlanType == planType && x.IsActive);

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(x => x.Region == region);

            var plans = await query
                .Include(x => x.Coupons)
                .OrderBy(x => x.Price)
                .ToListAsync();

            var allActiveCoupons = await _context.Coupons
                .Where(c => c.IsActive)
                .ToListAsync();

            return plans
                .Select(plan => ToResponseDto(plan, allActiveCoupons))
                .ToList();
        }

        private static MembershipPlanResponseDto ToResponseDto( MembershipPlan plan, List<Coupon> allActiveCoupons)
        {
            var planRegion = plan.Region?.Trim().ToLower();

            var couponCodes = allActiveCoupons
                .Where(c =>
                    c.IsActive
                    &&
                    c.PlanType == plan.PlanType
                    &&
                    // Specific plan OR All Plans
                    (c.PlanId == null || c.PlanId == plan.PlanId)
                    &&
                    // Specific region OR All Regions
                    (
                        string.Equals(
                            c.Region?.Trim(),
                            "all",
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        string.Equals(
                            c.Region?.Trim(),
                            "all regions",
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        string.Equals(
                            c.Region?.Trim(),
                            planRegion,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Select(c => c.Code)
                .Distinct()
                .ToList();

            return new MembershipPlanResponseDto
            {
                PlanId = plan.PlanId,
                PlanType = plan.PlanType,
                Region = plan.Region,
                PlanName = plan.PlanName,
                Description = plan.Description,
                Price = plan.Price,
                Period = plan.Period,
                Badge = plan.Badge,
                Features = plan.Features,
                IsActive = plan.IsActive,
                CouponCodes = couponCodes
            };
        }
    }
}