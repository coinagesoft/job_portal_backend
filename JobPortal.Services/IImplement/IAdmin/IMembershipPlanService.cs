using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Admin.MembershipPlan;
using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IMembershipPlanService
    {
        Task<CommonResponseDto> CreatePlanAsync(CreateMembershipPlanRequestDto request, Guid adminId);

        Task<CommonResponseDto> UpdatePlanAsync(UpdateMembershipPlanRequestDto request, Guid adminId);

        Task<CommonResponseDto> DeletePlanAsync(Guid planId, Guid adminId);

        // planType/region are optional filters — omit either to get all.
        Task<List<MembershipPlanResponseDto>> GetAllPlansAsync(PlanType? planType = null, string? region = null);

        Task<MembershipPlanResponseDto?> GetPlanByIdAsync(Guid planId);

        // Public/consumer-facing: active plans only, for a given type + region.
        Task<List<MembershipPlanResponseDto>> GetActivePlansAsync(PlanType planType, string? region = null);
    }
}