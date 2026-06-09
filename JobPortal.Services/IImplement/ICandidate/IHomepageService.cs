// ============================================================
//  JobPortal.Services/IImplement/IPublic/IHomepageService.cs
// ============================================================

using JobPortal.Application.DTOs.Public;

namespace JobPortal.Services.IImplement.IPublic;

public interface IHomepageService
{
    Task<HomepageResponseDto> GetHomepageDataAsync(HomepageRequestDto request);
}