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

    public class CreditConfigurationService : ICreditConfigurationService
    {
        private readonly AppDbContext _context;

        public CreditConfigurationService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<CreditConfigurationResponseDto?> GetConfigurationAsync()
        {
            // FIX: without an explicit order, Postgres does not guarantee which
            // row FirstOrDefault returns if more than one row has IsActive = true
            // (which can happen if UpdateConfigurationAsync ever ran when no
            // active row existed yet). Ordering by UpdatedAt (newest first) makes
            // this deterministic: we always read back whatever was saved last.
            var config =
                await _context.CreditConfigurations
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefaultAsync();

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

        public async Task<CommonResponseDto> UpdateConfigurationAsync(
                UpdateCreditConfigurationRequestDto request,
                Guid adminId)
        {
            // FIX: load ALL active rows (not just the first one) so we can
            // detect and self-heal a duplicate-row situation instead of
            // silently updating whichever row Postgres happens to hand back.
            var activeConfigs =
                await _context.CreditConfigurations
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.UpdatedAt)
                    .ToListAsync();

            CreditConfiguration config;

            if (activeConfigs.Count == 0)
            {
                config = new CreditConfiguration
                {
                    ConfigurationId = Guid.NewGuid(),
                    IsActive = true
                };

                _context.CreditConfigurations.Add(config);
            }
            else
            {
                // Keep the most recently updated row as the single source of
                // truth and deactivate any older duplicates so future GETs
                // can never pick a stale one again.
                config = activeConfigs[0];

                for (int i = 1; i < activeConfigs.Count; i++)
                {
                    activeConfigs[i].IsActive = false;
                }
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