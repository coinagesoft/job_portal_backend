using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{

    public class CreditPlanService : ICreditPlanService
    {
        private readonly AppDbContext _context;

        public CreditPlanService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommonResponseDto> CreatePlanAsync(
    CreateCreditPlanRequestDto request,
    Guid adminId)
        {
            var plan = new CreditPlan
            {
                PlanId = Guid.NewGuid(),
                PlanName = request.PlanName,
                Credits = request.Credits,
                Price = request.Price,
                ValidityMonths = request.ValidityMonths,
                Region = string.IsNullOrWhiteSpace(request.Region) ? "us" : request.Region,
                Bonus = request.Bonus,
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditPlans.Add(plan);

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Credit plan created successfully.",
                PlanId = plan.PlanId
            };
        }


        public async Task<CommonResponseDto> UpdatePlanAsync(
      UpdateCreditPlanRequestDto request,
      Guid adminId)
        {
            var plan = await _context.CreditPlans
                .FirstOrDefaultAsync(x => x.PlanId == request.PlanId);

            if (plan == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Plan not found."
                };
            }

            plan.PlanName = request.PlanName;
            plan.Credits = request.Credits;
            plan.Price = request.Price;
            plan.ValidityMonths = request.ValidityMonths;
            plan.Region = string.IsNullOrWhiteSpace(request.Region) ? plan.Region : request.Region;
            plan.Bonus = request.Bonus;
            plan.IsActive = request.IsActive;
            plan.UpdatedBy = adminId;

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Credit plan updated successfully.",
                PlanId = plan.PlanId
            };
        }




        public async Task<List<AdminCreditPlanResponseDto>> GetAllPlansAsync(Guid adminId, string? region = null)
        {
            var query = _context.CreditPlans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(x => x.Region == region);
            }

            return await query
                .OrderBy(x => x.Price)
                .Select(x =>
                    new AdminCreditPlanResponseDto
                    {
                        PlanId = x.PlanId,
                        PlanName = x.PlanName,
                        Credits = x.Credits,
                        Price = x.Price,
                        ValidityMonths =
                            x.ValidityMonths,
                        Region = x.Region,
                        Bonus = x.Bonus,
                        IsActive = x.IsActive
                    })
                .ToListAsync();
        }


        public async Task<CommonResponseDto> DeletePlanAsync(Guid planId, Guid adminId)
        {
            var plan =
                await _context.CreditPlans
                    .FirstOrDefaultAsync(x =>
                        x.PlanId == planId);

            if (plan == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Plan not found."
                };
            }

            // Employers who already purchased this plan hold a row in
            // EmployerPlanPurchase referencing it by PlanId. Hard-deleting
            // the plan out from under that purchase history would either
            // violate the FK or, if cascade delete is configured, silently
            // erase what those employers paid for. Guard against that
            // instead of letting it fail with a raw DB exception — steer
            // the admin to deactivate (soft-delete) in that case, which
            // still hides the plan from new purchases.
            var hasPurchaseHistory = await _context.EmployerPlanPurchase
                .AnyAsync(x => x.PlanId == planId);

            if (hasPurchaseHistory)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "This plan has already been purchased by one or more employers and can't be deleted. Deactivate it instead to hide it from new purchases.",
                    PlanId = plan.PlanId
                };
            }

            _context.CreditPlans.Remove(plan);

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Plan deleted successfully.",
                PlanId = planId
            };
        }


        public async Task<AdminCreditPlanResponseDto?> GetPlanByIdAsync(Guid planId, Guid adminId)
        {
            var plan = await _context.CreditPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PlanId == planId);

            if (plan == null)
                return null;

            return new AdminCreditPlanResponseDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                Price = plan.Price,
                ValidityMonths = plan.ValidityMonths,
                Region = plan.Region,
                Bonus = plan.Bonus,
                IsActive = plan.IsActive
            };
        }


    }
}