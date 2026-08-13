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
}