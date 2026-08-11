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
                Message = "Membership plan updated successfully."
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
                .OrderBy(x => x.Price)
                .ToListAsync();

            return plans.Select(ToResponseDto).ToList();
        }

        public async Task<MembershipPlanResponseDto?> GetPlanByIdAsync(Guid planId)
        {
            var plan = await _context.MembershipPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PlanId == planId);

            return plan == null ? null : ToResponseDto(plan);
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
                .OrderBy(x => x.Price)
                .ToListAsync();

            return plans.Select(ToResponseDto).ToList();
        }

        private static MembershipPlanResponseDto ToResponseDto(MembershipPlan plan) =>
            new MembershipPlanResponseDto
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
                IsActive = plan.IsActive
            };
    }
}