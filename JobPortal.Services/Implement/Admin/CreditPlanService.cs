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
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditPlans.Add(plan);

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Credit plan created successfully."
            };
        }


        public async Task<CommonResponseDto> UpdatePlanAsync(
      UpdateCreditPlanRequestDto request,
      Guid adminId)
        {
            var plan = new CreditPlan
            {
                PlanId = Guid.NewGuid(),
                PlanName = request.PlanName,
                Credits = request.Credits,
                Price = request.Price,
                ValidityMonths = request.ValidityMonths,
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditPlans.Add(plan);

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Credit plan created successfully."
            };
        }

     


        public async Task<List<CreditPlanResponseDto>>GetAllPlansAsync(Guid adminId)
        {
            return await _context.CreditPlans
                .OrderBy(x => x.Price)
                .Select(x =>
                    new CreditPlanResponseDto
                    {
                        PlanId = x.PlanId,
                        PlanName = x.PlanName,
                        Credits = x.Credits,
                        Price = x.Price,
                        ValidityMonths =
                            x.ValidityMonths,
                        IsActive = x.IsActive
                    })
                .ToListAsync();
        }


        public async Task<CommonResponseDto> DeletePlanAsync(Guid planId,Guid adminId)
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

            plan.IsActive = false;

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Plan deactivated."
            };
        }


        public async Task<CreditPlanResponseDto?>GetPlanByIdAsync(Guid planId,Guid adminId)
        {
            var plan = await _context.CreditPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PlanId == planId);

            if (plan == null)
                return null;

            return new CreditPlanResponseDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                Price = plan.Price,
                ValidityMonths = plan.ValidityMonths,
                IsActive = plan.IsActive
            };
        }
    }
}
