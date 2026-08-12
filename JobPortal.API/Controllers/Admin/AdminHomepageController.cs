// ============================================================
//  JobPortal.API/Controllers/Admin/AdminHomepageController.cs
//
//  Backs the Admin "Homepage Management" screen:
//  https://job-portal-admin-gray.vercel.app/admin/homepage-management
//
//  One controller for every section (Hero / Industries / Statistics /
//  Locations / Roles / Registration Industries / Departments /
//  Trade Categories / Suggestions) — deliberately kept in one file
//  since each section is a thin CRUD wrapper around one service.
// ============================================================

using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.Homepage;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/homepage")]
    //[Authorize] // TODO: apply your Admin role/policy here
    public class AdminHomepageController : ControllerBase
    {
        private readonly IAdminHomepageManagementService _service;

        public AdminHomepageController(IAdminHomepageManagementService service)
        {
            _service = service;
        }

        /// <summary>
        /// Reads the admin id from the "AdminId" JWT claim. Falls back to
        /// null (rather than a fake id) when unauthenticated in dev, since
        /// UpdatedBy/ReviewedBy are nullable.
        /// </summary>
        private Guid? GetAdminId()
        {
            var claim = User.FindFirst("AdminId")?.Value;
            return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
        }

        // ============================================================
        // Hero Section
        // ============================================================

        [HttpGet("hero")]
        public async Task<IActionResult> GetHero()
        {
            var result = await _service.GetHeroAsync();
            return Ok(result);
        }

        [HttpPut("hero")]
        [AuditLog("Update Hero Section", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateHero([FromBody] UpdateHeroSectionRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.UpdateHeroAsync(request, GetAdminId());
            return Ok(new { success = true, message = "Hero section updated.", data = result });
        }

        [HttpPost("hero/banner")]
        [AuditLog("Upload Hero Banner", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UploadHeroBanner(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            var result = await _service.UploadHeroBannerAsync(file, GetAdminId());
            return Ok(new { success = true, message = "Banner uploaded.", data = result });
        }

        // ============================================================
        // Browse by Industry
        // ============================================================

        [HttpGet("industries")]
        public async Task<IActionResult> GetIndustries()
        {
            var result = await _service.GetIndustriesAsync();
            return Ok(result);
        }

        [HttpPost("industries")]
        [AuditLog("Add Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddIndustry([FromBody] CreateIndustryRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateIndustryAsync(request);
            return Ok(new { success = true, message = "Industry added.", data = result });
        }

        [HttpPut("industries/{industryId:guid}")]
        [AuditLog("Update Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateIndustry(Guid industryId, [FromBody] UpdateIndustryRequestDto request)
        {
            var result = await _service.UpdateIndustryAsync(industryId, request);
            if (result == null) return NotFound(new { success = false, message = "Industry not found." });

            return Ok(new { success = true, message = "Industry updated.", data = result });
        }

        [HttpDelete("industries/{industryId:guid}")]
        [AuditLog("Delete Industry", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteIndustry(Guid industryId)
        {
            var deleted = await _service.DeleteIndustryAsync(industryId);
            if (!deleted) return NotFound(new { success = false, message = "Industry not found." });

            return Ok(new { success = true, message = "Industry deleted." });
        }

        [HttpPatch("industries/{industryId:guid}/toggle")]
        [AuditLog("Toggle Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleIndustry(Guid industryId)
        {
            var result = await _service.ToggleIndustryAsync(industryId);
            if (result == null) return NotFound(new { success = false, message = "Industry not found." });

            return Ok(new { success = true, message = "Industry status updated.", data = result });
        }

        [HttpPatch("industries/{industryId:guid}/dropdown")]
        [AuditLog("Toggle Industry Dropdown Visibility", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleIndustryDropdown(Guid industryId)
        {
            var result = await _service.ToggleIndustryDropdownAsync(industryId);
            if (result == null) return NotFound(new { success = false, message = "Industry not found." });

            return Ok(new { success = true, message = "Dropdown visibility updated.", data = result });
        }

        // ============================================================
        // Hiring Statistics
        // ============================================================

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var result = await _service.GetStatisticsAsync();
            return Ok(result);
        }

        [HttpPut("statistics")]
        [AuditLog("Update Hiring Statistics", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateStatistics([FromBody] UpdateStatisticsRequestDto request)
        {
            var result = await _service.UpdateStatisticsAsync(request, GetAdminId());
            return Ok(new { success = true, message = "Statistics updated.", data = result });
        }

        // ============================================================
        // Browse Jobs by Location
        // ============================================================

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            var result = await _service.GetLocationsAsync();
            return Ok(result);
        }

        [HttpPost("locations")]
        [AuditLog("Add Location", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddLocation([FromBody] CreateLocationRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateLocationAsync(request);
            return Ok(new { success = true, message = "Location added.", data = result });
        }

        [HttpPut("locations/{locationId:guid}")]
        [AuditLog("Update Location", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateLocation(Guid locationId, [FromBody] UpdateLocationRequestDto request)
        {
            var result = await _service.UpdateLocationAsync(locationId, request);
            if (result == null) return NotFound(new { success = false, message = "Location not found." });

            return Ok(new { success = true, message = "Location updated.", data = result });
        }

        [HttpDelete("locations/{locationId:guid}")]
        [AuditLog("Delete Location", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteLocation(Guid locationId)
        {
            var deleted = await _service.DeleteLocationAsync(locationId);
            if (!deleted) return NotFound(new { success = false, message = "Location not found." });

            return Ok(new { success = true, message = "Location deleted." });
        }

        [HttpPatch("locations/{locationId:guid}/toggle")]
        [AuditLog("Toggle Location", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleLocation(Guid locationId)
        {
            var result = await _service.ToggleLocationAsync(locationId);
            if (result == null) return NotFound(new { success = false, message = "Location not found." });

            return Ok(new { success = true, message = "Location status updated.", data = result });
        }

        [HttpPost("locations/{locationId:guid}/image")]
        [AuditLog("Upload Location Image", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UploadLocationImage(Guid locationId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            var result = await _service.UploadLocationImageAsync(locationId, file);
            if (result == null) return NotFound(new { success = false, message = "Location not found." });

            return Ok(new { success = true, message = "Location image uploaded.", data = result });
        }

        // ============================================================
        // Browse Jobs by Role
        // ============================================================

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _service.GetRolesAsync();
            return Ok(result);
        }

        [HttpPost("roles")]
        [AuditLog("Add Role", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddRole([FromBody] CreateRoleRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateRoleAsync(request);
            return Ok(new { success = true, message = "Role added.", data = result });
        }

        [HttpPut("roles/{roleId:guid}")]
        [AuditLog("Update Role", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequestDto request)
        {
            var result = await _service.UpdateRoleAsync(roleId, request);
            if (result == null) return NotFound(new { success = false, message = "Role not found." });

            return Ok(new { success = true, message = "Role updated.", data = result });
        }

        [HttpDelete("roles/{roleId:guid}")]
        [AuditLog("Delete Role", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteRole(Guid roleId)
        {
            var deleted = await _service.DeleteRoleAsync(roleId);
            if (!deleted) return NotFound(new { success = false, message = "Role not found." });

            return Ok(new { success = true, message = "Role deleted." });
        }

        [HttpPatch("roles/{roleId:guid}/toggle")]
        [AuditLog("Toggle Role", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleRole(Guid roleId)
        {
            var result = await _service.ToggleRoleAsync(roleId);
            if (result == null) return NotFound(new { success = false, message = "Role not found." });

            return Ok(new { success = true, message = "Role status updated.", data = result });
        }

        // ============================================================
        // Registration Industries
        // ============================================================

        [HttpGet("registration-industries")]
        public async Task<IActionResult> GetRegistrationIndustries()
        {
            var result = await _service.GetRegistrationIndustriesAsync();
            return Ok(result);
        }

        [HttpPost("registration-industries")]
        [AuditLog("Add Registration Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddRegistrationIndustry([FromBody] CreateNamedListItemRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateRegistrationIndustryAsync(request);
            return Ok(new { success = true, message = "Registration industry added.", data = result });
        }

        [HttpPut("registration-industries/{id:guid}")]
        [AuditLog("Update Registration Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateRegistrationIndustry(Guid id, [FromBody] UpdateNamedListItemRequestDto request)
        {
            var result = await _service.UpdateRegistrationIndustryAsync(id, request);
            if (result == null) return NotFound(new { success = false, message = "Registration industry not found." });

            return Ok(new { success = true, message = "Registration industry updated.", data = result });
        }

        [HttpDelete("registration-industries/{id:guid}")]
        [AuditLog("Delete Registration Industry", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteRegistrationIndustry(Guid id)
        {
            var deleted = await _service.DeleteRegistrationIndustryAsync(id);
            if (!deleted) return NotFound(new { success = false, message = "Registration industry not found." });

            return Ok(new { success = true, message = "Registration industry deleted." });
        }

        [HttpPatch("registration-industries/{id:guid}/toggle")]
        [AuditLog("Toggle Registration Industry", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleRegistrationIndustry(Guid id)
        {
            var result = await _service.ToggleRegistrationIndustryAsync(id);
            if (result == null) return NotFound(new { success = false, message = "Registration industry not found." });

            return Ok(new { success = true, message = "Status updated.", data = result });
        }

        // ============================================================
        // Departments
        // ============================================================

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var result = await _service.GetDepartmentsAsync();
            return Ok(result);
        }

        [HttpPost("departments")]
        [AuditLog("Add Department", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddDepartment([FromBody] CreateNamedListItemRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateDepartmentAsync(request);
            return Ok(new { success = true, message = "Department added.", data = result });
        }

        [HttpPut("departments/{id:guid}")]
        [AuditLog("Update Department", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateNamedListItemRequestDto request)
        {
            var result = await _service.UpdateDepartmentAsync(id, request);
            if (result == null) return NotFound(new { success = false, message = "Department not found." });

            return Ok(new { success = true, message = "Department updated.", data = result });
        }

        [HttpDelete("departments/{id:guid}")]
        [AuditLog("Delete Department", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var deleted = await _service.DeleteDepartmentAsync(id);
            if (!deleted) return NotFound(new { success = false, message = "Department not found." });

            return Ok(new { success = true, message = "Department deleted." });
        }

        [HttpPatch("departments/{id:guid}/toggle")]
        [AuditLog("Toggle Department", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleDepartment(Guid id)
        {
            var result = await _service.ToggleDepartmentAsync(id);
            if (result == null) return NotFound(new { success = false, message = "Department not found." });

            return Ok(new { success = true, message = "Status updated.", data = result });
        }

        // ============================================================
        // Trade Categories
        // ============================================================

        [HttpGet("trade-categories")]
        public async Task<IActionResult> GetTradeCategories()
        {
            var result = await _service.GetTradeCategoriesAsync();
            return Ok(result);
        }

        [HttpPost("trade-categories")]
        [AuditLog("Add Trade Category", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> AddTradeCategory([FromBody] CreateNamedListItemRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateTradeCategoryAsync(request);
            return Ok(new { success = true, message = "Trade category added.", data = result });
        }

        [HttpPut("trade-categories/{id:guid}")]
        [AuditLog("Update Trade Category", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> UpdateTradeCategory(Guid id, [FromBody] UpdateNamedListItemRequestDto request)
        {
            var result = await _service.UpdateTradeCategoryAsync(id, request);
            if (result == null) return NotFound(new { success = false, message = "Trade category not found." });

            return Ok(new { success = true, message = "Trade category updated.", data = result });
        }

        [HttpDelete("trade-categories/{id:guid}")]
        [AuditLog("Delete Trade Category", "Homepage Management", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteTradeCategory(Guid id)
        {
            var deleted = await _service.DeleteTradeCategoryAsync(id);
            if (!deleted) return NotFound(new { success = false, message = "Trade category not found." });

            return Ok(new { success = true, message = "Trade category deleted." });
        }

        [HttpPatch("trade-categories/{id:guid}/toggle")]
        [AuditLog("Toggle Trade Category", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ToggleTradeCategory(Guid id)
        {
            var result = await _service.ToggleTradeCategoryAsync(id);
            if (result == null) return NotFound(new { success = false, message = "Trade category not found." });

            return Ok(new { success = true, message = "Status updated.", data = result });
        }

        // ============================================================
        // Suggestions
        // ============================================================

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions()
        {
            var result = await _service.GetSuggestionsAsync();
            return Ok(result);
        }

        [HttpDelete("suggestions/{id:guid}")]
        [AuditLog("Delete Suggestion", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> DeleteSuggestion(Guid id)
        {
            var deleted = await _service.DeleteSuggestionAsync(id);
            if (!deleted) return NotFound(new { success = false, message = "Suggestion not found." });

            return Ok(new { success = true, message = "Suggestion deleted." });
        }

        [HttpPatch("suggestions/{id:guid}/approve")]
        [AuditLog("Approve Suggestion", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> ApproveSuggestion(Guid id, [FromBody] ReviewSuggestionRequestDto? request)
        {
            var result = await _service.ApproveSuggestionAsync(id, request ?? new ReviewSuggestionRequestDto(), GetAdminId());
            if (result == null) return NotFound(new { success = false, message = "Suggestion not found." });

            return Ok(new { success = true, message = "Suggestion approved.", data = result });
        }

        [HttpPatch("suggestions/{id:guid}/reject")]
        [AuditLog("Reject Suggestion", "Homepage Management", AuditSeverity.Info)]
        public async Task<IActionResult> RejectSuggestion(Guid id, [FromBody] ReviewSuggestionRequestDto? request)
        {
            var result = await _service.RejectSuggestionAsync(id, request ?? new ReviewSuggestionRequestDto(), GetAdminId());
            if (result == null) return NotFound(new { success = false, message = "Suggestion not found." });

            return Ok(new { success = true, message = "Suggestion rejected.", data = result });
        }
    }
}