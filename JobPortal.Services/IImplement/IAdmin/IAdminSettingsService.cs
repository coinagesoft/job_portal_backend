using JobPortal.Application.DTOs.Admin.Settings;
using System;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminSettingsService
    {
        /// <summary>
        /// Returns the calling admin's settings, creating a default row
        /// (matching the frontend's defaults) the first time it's read.
        /// </summary>
        Task<AdminSettingsDto> GetSettingsAsync(Guid adminId);

        /// <summary>Saves the calling admin's Default Language / Session Timeout.</summary>
        Task<(bool Success, string? Error, AdminSettingsDto? Data)> UpdateSettingsAsync(
            Guid adminId, UpdateAdminSettingsRequestDto request);
    }
}