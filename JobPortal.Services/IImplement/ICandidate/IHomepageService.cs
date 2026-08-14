// ============================================================
//  JobPortal.Services/IImplement/IPublic/IHomepageService.cs
// ============================================================

using JobPortal.Application.DTOs.Public;

namespace JobPortal.Services.IImplement.IPublic;

public interface IHomepageService
{
    Task<HomepageResponseDto> GetHomepageDataAsync(HomepageRequestDto request);

    /// <summary>Records a "this isn't in your list" suggestion for admin review.</summary>
    Task<SubmitSuggestionResponseDto> SubmitSuggestionAsync(SubmitSuggestionRequestDto request, Guid? submittedByUserId);

    /// <summary>
    /// Everything managed from the Admin "Homepage Management" screen
    /// (Hero, Industries, Statistics, Locations, Roles, Registration
    /// Industries, Departments, Trade Categories) — active items only,
    /// in display order. Backs GET api/public/homepage/data.
    /// </summary>
    Task<PublicHomepageContentResponseDto> GetHomepageManagementDataAsync();
}