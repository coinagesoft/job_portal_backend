using JobPortal.Application.DTOs.Admin.CreditWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterCreditPlanService
    {
        Task<CommonResponseDto> BuyPlanAsync(
            Guid employerId,
            Guid planId);
    }
}
