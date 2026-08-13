// ============================================================
//  JobPortal.Services/IImplement/IAdmin/IAdminHomepageManagementService.cs
//
//  One service backs the whole "Homepage Management" admin screen —
//  Hero / Industries / Statistics / Locations / Roles /
//  Registration Industries / Departments / Trade Categories / Suggestions.
// ============================================================

using JobPortal.Application.DTOs.Admin.Homepage;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminHomepageManagementService
    {
        // Hero Section
        Task<HeroSectionDto> GetHeroAsync();
        Task<HeroSectionDto> UpdateHeroAsync(UpdateHeroSectionRequestDto request, Guid? adminId);
        Task<HeroSectionDto> UploadHeroBannerAsync(IFormFile file, Guid? adminId);

        // Browse by Industry
        Task<List<IndustryDto>> GetIndustriesAsync();
        Task<IndustryDto> CreateIndustryAsync(CreateIndustryRequestDto request);
        Task<IndustryDto?> UpdateIndustryAsync(Guid industryId, UpdateIndustryRequestDto request);
        Task<bool> DeleteIndustryAsync(Guid industryId);
        Task<IndustryDto?> ToggleIndustryAsync(Guid industryId);
        Task<IndustryDto?> ToggleIndustryDropdownAsync(Guid industryId);

        // Hiring Statistics
        Task<StatisticsDto> GetStatisticsAsync();
        Task<StatisticsDto> UpdateStatisticsAsync(UpdateStatisticsRequestDto request, Guid? adminId);

        // Browse Jobs by Location
        Task<List<LocationDto>> GetLocationsAsync();
        Task<LocationDto> CreateLocationAsync(CreateLocationRequestDto request);
        Task<LocationDto?> UpdateLocationAsync(Guid locationId, UpdateLocationRequestDto request);
        Task<bool> DeleteLocationAsync(Guid locationId);
        Task<LocationDto?> ToggleLocationAsync(Guid locationId);
        Task<LocationDto?> UploadLocationImageAsync(Guid locationId, IFormFile file);

        // Browse Jobs by Role
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request);
        Task<RoleDto?> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto request);
        Task<bool> DeleteRoleAsync(Guid roleId);
        Task<RoleDto?> ToggleRoleAsync(Guid roleId);

        // Registration Industries
        Task<List<NamedListItemDto>> GetRegistrationIndustriesAsync();
        Task<NamedListItemDto> CreateRegistrationIndustryAsync(CreateNamedListItemRequestDto request);
        Task<NamedListItemDto?> UpdateRegistrationIndustryAsync(Guid id, UpdateNamedListItemRequestDto request);
        Task<bool> DeleteRegistrationIndustryAsync(Guid id);
        Task<NamedListItemDto?> ToggleRegistrationIndustryAsync(Guid id);

        // Departments
        Task<List<NamedListItemDto>> GetDepartmentsAsync();
        Task<NamedListItemDto> CreateDepartmentAsync(CreateNamedListItemRequestDto request);
        Task<NamedListItemDto?> UpdateDepartmentAsync(Guid id, UpdateNamedListItemRequestDto request);
        Task<bool> DeleteDepartmentAsync(Guid id);
        Task<NamedListItemDto?> ToggleDepartmentAsync(Guid id);

        // Trade Categories
        Task<List<NamedListItemDto>> GetTradeCategoriesAsync();
        Task<NamedListItemDto> CreateTradeCategoryAsync(CreateNamedListItemRequestDto request);
        Task<NamedListItemDto?> UpdateTradeCategoryAsync(Guid id, UpdateNamedListItemRequestDto request);
        Task<bool> DeleteTradeCategoryAsync(Guid id);
        Task<NamedListItemDto?> ToggleTradeCategoryAsync(Guid id);

        // Suggestions
        Task<List<SuggestionDto>> GetSuggestionsAsync();
        Task<bool> DeleteSuggestionAsync(Guid id);
        Task<SuggestionDto?> ApproveSuggestionAsync(Guid id, ReviewSuggestionRequestDto request, Guid? adminId);
        Task<SuggestionDto?> RejectSuggestionAsync(Guid id, ReviewSuggestionRequestDto request, Guid? adminId);
    }
}