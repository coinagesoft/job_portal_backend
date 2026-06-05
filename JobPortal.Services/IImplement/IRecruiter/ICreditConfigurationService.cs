using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ICreditConfigurationService
    {
        Task<CreditConfigurationResponseDto?>
            GetConfigurationAsync();

        Task<CommonResponseDto>
            UpdateConfigurationAsync(
                UpdateCreditConfigurationRequestDto request,
                Guid adminId);
    }
}
