using JobPortal.Application.DTOs.Admin.CreditWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{


    public interface ICreditPlanService
    {
        Task<CommonResponseDto> CreatePlanAsync(CreateCreditPlanRequestDto request, Guid adminId);

        Task<CommonResponseDto> UpdatePlanAsync(UpdateCreditPlanRequestDto request, Guid adminId);

        Task<CommonResponseDto> DeletePlanAsync(Guid planId, Guid adminId);

        Task<List<CreditPlanResponseDto>> GetAllPlansAsync(Guid adminId, string? region = null);

        Task<CreditPlanResponseDto?> GetPlanByIdAsync(Guid planId, Guid adminId);


    }
}