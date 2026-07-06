using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
   
    public class CreditConfigurationService: ICreditConfigurationService
    {
        private readonly AppDbContext _context;

        public CreditConfigurationService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<CreditConfigurationResponseDto?>
            GetConfigurationAsync()
        {
            var config =
                await _context.CreditConfigurations
                    .FirstOrDefaultAsync(x => x.IsActive);

            if (config == null)
                return null;

            return new CreditConfigurationResponseDto
            {
                ConfigurationId = config.ConfigurationId,
                ProfileUnlockCredits =
                    config.ProfileUnlockCredits,
                CvDownloadCredits =
                    config.CvDownloadCredits,
                CandidateAccessDays =
                    config.CandidateAccessDays,
                IsActive = config.IsActive,
                UpdatedAt = config.UpdatedAt,
                UpdatedBy = config.UpdatedBy
            };
        }

        public async Task<CommonResponseDto>
            UpdateConfigurationAsync(
                UpdateCreditConfigurationRequestDto request,
                Guid adminId)
        {
            var config =
                await _context.CreditConfigurations
                    .FirstOrDefaultAsync(x => x.IsActive);

            if (config == null)
            {
                config = new CreditConfiguration
                {
                    ConfigurationId = Guid.NewGuid(),
                    IsActive = true
                };

                _context.CreditConfigurations.Add(config);
            }

            config.ProfileUnlockCredits =
                request.ProfileUnlockCredits;

            config.CvDownloadCredits =
                request.CvDownloadCredits;

            config.CandidateAccessDays =
                request.CandidateAccessDays;

            config.UpdatedAt =
                DateTime.UtcNow;

            config.UpdatedBy =
                adminId;

            await _context.SaveChangesAsync();

            return new CommonResponseDto
            {
                Success = true,
                Message =
                    "Credit configuration updated successfully."
            };
        }
    }
}
