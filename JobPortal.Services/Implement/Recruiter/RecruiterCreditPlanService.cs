using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCreditPlanService : IRecruiterCreditPlanService
    {

        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterCreditPlanService> _logger;

        public RecruiterCreditPlanService(
            AppDbContext context,
            ILogger<RecruiterCreditPlanService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommonResponseDto> BuyPlanAsync(Guid employerId,Guid planId)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var plan = await _context.CreditPlans
                .FirstOrDefaultAsync(x =>
                    x.PlanId == planId &&
                    x.IsActive);

            if (plan == null)
            {
                return new CommonResponseDto
                {
                    Success = false,
                    Message = "Plan not found."
                };
            }

            var wallet = await _context.CreditWallets
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId);

            if (wallet == null)
            {
                wallet = new CreditWallet
                {
                    Wallet_Id = Guid.NewGuid(),
                    EmployerId = employerId,
                    CreditBalance = plan.Credits,
                    PackageName = plan.PlanName,
                    PackExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                    SharedWallet = true,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CreditWallets.Add(wallet);
            }
            else
            {
                wallet.CreditBalance += plan.Credits;

                wallet.PackageName = plan.PlanName;

                var baseDate =
                    wallet.PackExpiresAt > DateTime.UtcNow
                    ? wallet.PackExpiresAt.Value
                    : DateTime.UtcNow;

                wallet.PackExpiresAt =
                    baseDate.AddMonths(plan.ValidityMonths);

                wallet.UpdatedAt = DateTime.UtcNow;
            }

            var purchase = new EmployerPlanPurchase
            {
                EmployerCreditPlanId = Guid.NewGuid(),
                EmployerId = employerId,
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Credits = plan.Credits,
                Price = plan.Price,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(plan.ValidityMonths),
                IsActive = true,
                AssignedBy = employerId
            };

            _context.EmployerPlanPurchase.Add(purchase);

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message = "Plan purchased successfully."
            };
        }
    }
}
