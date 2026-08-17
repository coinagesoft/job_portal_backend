using JobPortal.Application.DTOs.Admin.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    public class AdminSettingsService : IAdminSettingsService
    {
        // Kept in sync with the option lists on the /admin/settings screen.
        private static readonly string[] AllowedLanguages =
        {
            "English (US)", "English (UK)", "Hindi", "French", "Spanish", "German"
        };

        private static readonly string[] AllowedSessionTimeouts =
        {
            "15 Minutes", "30 Minutes", "1 Hour", "2 Hours", "Never"
        };

        private readonly AppDbContext _context;

        public AdminSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminSettingsDto> GetSettingsAsync(Guid adminId)
        {
            var settings = await _context.AdminUserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdminId == adminId);

            if (settings == null)
            {
                // No row yet — hand back the same defaults the frontend
                // ships with, without persisting until the admin saves.
                return new AdminSettingsDto
                {
                    Language = "English (US)",
                    SessionTimeout = "30 Minutes",
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return Map(settings);
        }

        public async Task<(bool Success, string? Error, AdminSettingsDto? Data)> UpdateSettingsAsync(
            Guid adminId, UpdateAdminSettingsRequestDto request)
        {
            if (!AllowedLanguages.Contains(request.Language))
                return (false, "Unsupported language selection.", null);

            if (!AllowedSessionTimeouts.Contains(request.SessionTimeout))
                return (false, "Unsupported session timeout selection.", null);

            var settings = await _context.AdminUserSettings
                .FirstOrDefaultAsync(x => x.AdminId == adminId);

            if (settings == null)
            {
                settings = new AdminUserSettings
                {
                    SettingsId = Guid.NewGuid(),
                    AdminId = adminId
                };
                _context.AdminUserSettings.Add(settings);
            }

            settings.Language = request.Language;
            settings.SessionTimeout = request.SessionTimeout;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, null, Map(settings));
        }

        private static AdminSettingsDto Map(AdminUserSettings settings) => new AdminSettingsDto
        {
            Language = settings.Language,
            SessionTimeout = settings.SessionTimeout,
            UpdatedAt = settings.UpdatedAt
        };
    }
}