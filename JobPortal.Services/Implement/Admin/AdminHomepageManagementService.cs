// ============================================================
//  JobPortal.Services/Implement/Admin/AdminHomepageManagementService.cs
// ============================================================

using JobPortal.Application.DTOs.Admin;
using JobPortal.Application.DTOs.Admin.Homepage;
using JobPortal.Domain.Entities.Homepage;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter; // IFileStorageService
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Admin
{
    public class AdminHomepageManagementService : IAdminHomepageManagementService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AdminHomepageManagementService> _logger;

        public AdminHomepageManagementService(
            AppDbContext context,
            IFileStorageService fileStorageService,
            ILogger<AdminHomepageManagementService> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        // ============================================================
        // Hero Section (singleton)
        // ============================================================

        public async Task<HeroSectionDto> GetHeroAsync()
        {
            var hero = await GetOrCreateHeroAsync();
            return MapHero(hero);
        }

        public async Task<HeroSectionDto> UpdateHeroAsync(UpdateHeroSectionRequestDto request, Guid? adminId)
        {
            var hero = await GetOrCreateHeroAsync();

            hero.Headline = request.Headline;
            hero.Subheadline = request.Subheadline;
            hero.SearchPlaceholder = request.SearchPlaceholder;
            hero.CtaText = request.CtaText;
            hero.CtaLink = request.CtaLink;
            hero.UpdatedBy = adminId;
            hero.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapHero(hero);
        }

        public async Task<HeroSectionDto> UploadHeroBannerAsync(IFormFile file, Guid? adminId)
        {
            var hero = await GetOrCreateHeroAsync();

            var upload = await _fileStorageService.UploadImageAsync(file, "homepage/hero");

            hero.BannerImageUrl = upload.Url;
            hero.BannerImagePublicId = upload.PublicId;
            hero.UpdatedBy = adminId;
            hero.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapHero(hero);
        }

        private async Task<HomepageHero> GetOrCreateHeroAsync()
        {
            var hero = await _context.HomepageHeroes.FirstOrDefaultAsync();

            if (hero != null)
                return hero;

            hero = new HomepageHero
            {
                HeroId = Guid.NewGuid(),
                Headline = "Find Your Next Job Opportunity",
                Subheadline = "Search thousands of blue-collar and skilled trade jobs across India and the Gulf.",
                SearchPlaceholder = "Search by job title, skill or company",
                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageHeroes.Add(hero);
            await _context.SaveChangesAsync();

            return hero;
        }

        private static HeroSectionDto MapHero(HomepageHero h) => new()
        {
            HeroId = h.HeroId,
            Headline = h.Headline,
            Subheadline = h.Subheadline,
            SearchPlaceholder = h.SearchPlaceholder,
            CtaText = h.CtaText,
            CtaLink = h.CtaLink,
            BannerImageUrl = h.BannerImageUrl,
            UpdatedAt = h.UpdatedAt
        };

        // ============================================================
        // Browse by Industry
        // ============================================================

        public async Task<List<IndustryDto>> GetIndustriesAsync()
        {
            var items = await _context.HomepageIndustries
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return items.Select(MapIndustry).ToList();
        }

        public async Task<IndustryDto> CreateIndustryAsync(CreateIndustryRequestDto request)
        {
            var maxOrder = await _context.HomepageIndustries.MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var entity = new HomepageIndustry
            {
                IndustryId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Slug = Slugify(request.Name),
                IconUrl = request.IconUrl,
                JobCountOverride = request.JobCountOverride,
                ShowInDropdown = request.ShowInDropdown,
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageIndustries.Add(entity);
            await _context.SaveChangesAsync();

            return MapIndustry(entity);
        }

        public async Task<IndustryDto?> UpdateIndustryAsync(Guid industryId, UpdateIndustryRequestDto request)
        {
            var entity = await _context.HomepageIndustries.FirstOrDefaultAsync(x => x.IndustryId == industryId);
            if (entity == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                entity.Name = request.Name.Trim();
                entity.Slug = Slugify(request.Name);
            }
            if (request.IconUrl != null) entity.IconUrl = request.IconUrl;
            if (request.JobCountOverride.HasValue) entity.JobCountOverride = request.JobCountOverride;
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder.Value;

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapIndustry(entity);
        }

        public async Task<bool> DeleteIndustryAsync(Guid industryId)
        {
            var entity = await _context.HomepageIndustries.FirstOrDefaultAsync(x => x.IndustryId == industryId);
            if (entity == null) return false;

            _context.HomepageIndustries.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IndustryDto?> ToggleIndustryAsync(Guid industryId)
        {
            var entity = await _context.HomepageIndustries.FirstOrDefaultAsync(x => x.IndustryId == industryId);
            if (entity == null) return null;

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapIndustry(entity);
        }

        public async Task<IndustryDto?> ToggleIndustryDropdownAsync(Guid industryId)
        {
            var entity = await _context.HomepageIndustries.FirstOrDefaultAsync(x => x.IndustryId == industryId);
            if (entity == null) return null;

            entity.ShowInDropdown = !entity.ShowInDropdown;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapIndustry(entity);
        }

        private static IndustryDto MapIndustry(HomepageIndustry x) => new()
        {
            IndustryId = x.IndustryId,
            Name = x.Name,
            Slug = x.Slug,
            IconUrl = x.IconUrl,
            JobCountOverride = x.JobCountOverride,
            DisplayOrder = x.DisplayOrder,
            IsActive = x.IsActive,
            ShowInDropdown = x.ShowInDropdown
        };

        // ============================================================
        // Hiring Statistics (singleton)
        // ============================================================

        public async Task<StatisticsDto> GetStatisticsAsync()
        {
            var stats = await GetOrCreateStatisticsAsync();
            return new StatisticsDto { Items = stats.Items, UpdatedAt = stats.UpdatedAt };
        }

        public async Task<StatisticsDto> UpdateStatisticsAsync(UpdateStatisticsRequestDto request, Guid? adminId)
        {
            var stats = await GetOrCreateStatisticsAsync();

            stats.Items = request.Items
                .Select((item, i) => new HomepageStatItem
                {
                    Label = item.Label,
                    Value = item.Value,
                    Suffix = item.Suffix,
                    IconSlug = item.IconSlug,
                    DisplayOrder = item.DisplayOrder != 0 ? item.DisplayOrder : i
                })
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            stats.UpdatedAt = DateTime.UtcNow;
            stats.UpdatedBy = adminId;

            await _context.SaveChangesAsync();

            return new StatisticsDto { Items = stats.Items, UpdatedAt = stats.UpdatedAt };
        }

        private async Task<HomepageStatistics> GetOrCreateStatisticsAsync()
        {
            var stats = await _context.HomepageStatistics.FirstOrDefaultAsync();
            if (stats != null) return stats;

            stats = new HomepageStatistics
            {
                StatisticsId = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow,
                Items = new List<HomepageStatItem>
                {
                    new() { Label = "Active Jobs", Value = "0", DisplayOrder = 0 },
                    new() { Label = "Companies Hiring", Value = "0", DisplayOrder = 1 },
                    new() { Label = "Registered Candidates", Value = "0", DisplayOrder = 2 },
                    new() { Label = "Successful Placements", Value = "0", DisplayOrder = 3 }
                }
            };

            _context.HomepageStatistics.Add(stats);
            await _context.SaveChangesAsync();

            return stats;
        }

        // ============================================================
        // Browse Jobs by Location
        // ============================================================

        public async Task<List<LocationDto>> GetLocationsAsync()
        {
            var items = await _context.HomepageLocations
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return items.Select(MapLocation).ToList();
        }

        public async Task<LocationDto> CreateLocationAsync(CreateLocationRequestDto request)
        {
            var maxOrder = await _context.HomepageLocations.MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var entity = new HomepageLocation
            {
                LocationId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Country = request.Country,
                JobCountOverride = request.JobCountOverride,
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageLocations.Add(entity);
            await _context.SaveChangesAsync();

            return MapLocation(entity);
        }

        public async Task<LocationDto?> UpdateLocationAsync(Guid locationId, UpdateLocationRequestDto request)
        {
            var entity = await _context.HomepageLocations.FirstOrDefaultAsync(x => x.LocationId == locationId);
            if (entity == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (request.Country != null) entity.Country = request.Country;
            if (request.JobCountOverride.HasValue) entity.JobCountOverride = request.JobCountOverride;
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder.Value;

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapLocation(entity);
        }

        public async Task<bool> DeleteLocationAsync(Guid locationId)
        {
            var entity = await _context.HomepageLocations.FirstOrDefaultAsync(x => x.LocationId == locationId);
            if (entity == null) return false;

            _context.HomepageLocations.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LocationDto?> ToggleLocationAsync(Guid locationId)
        {
            var entity = await _context.HomepageLocations.FirstOrDefaultAsync(x => x.LocationId == locationId);
            if (entity == null) return null;

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapLocation(entity);
        }

        public async Task<LocationDto?> UploadLocationImageAsync(Guid locationId, IFormFile file)
        {
            var entity = await _context.HomepageLocations.FirstOrDefaultAsync(x => x.LocationId == locationId);
            if (entity == null) return null;

            var upload = await _fileStorageService.UploadImageAsync(file, "homepage/locations");

            entity.ImageUrl = upload.Url;
            entity.ImagePublicId = upload.PublicId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapLocation(entity);
        }

        private static LocationDto MapLocation(HomepageLocation x) => new()
        {
            LocationId = x.LocationId,
            Name = x.Name,
            Country = x.Country,
            ImageUrl = x.ImageUrl,
            JobCountOverride = x.JobCountOverride,
            DisplayOrder = x.DisplayOrder,
            IsActive = x.IsActive
        };

        // ============================================================
        // Browse Jobs by Role
        // ============================================================

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var items = await _context.HomepageRoles
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return items.Select(MapRole).ToList();
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request)
        {
            var maxOrder = await _context.HomepageRoles.MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var entity = new HomepageRole
            {
                RoleId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                IconUrl = request.IconUrl,
                JobCountOverride = request.JobCountOverride,
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageRoles.Add(entity);
            await _context.SaveChangesAsync();

            return MapRole(entity);
        }

        public async Task<RoleDto?> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto request)
        {
            var entity = await _context.HomepageRoles.FirstOrDefaultAsync(x => x.RoleId == roleId);
            if (entity == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (request.IconUrl != null) entity.IconUrl = request.IconUrl;
            if (request.JobCountOverride.HasValue) entity.JobCountOverride = request.JobCountOverride;
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder.Value;

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapRole(entity);
        }

        public async Task<bool> DeleteRoleAsync(Guid roleId)
        {
            var entity = await _context.HomepageRoles.FirstOrDefaultAsync(x => x.RoleId == roleId);
            if (entity == null) return false;

            _context.HomepageRoles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<RoleDto?> ToggleRoleAsync(Guid roleId)
        {
            var entity = await _context.HomepageRoles.FirstOrDefaultAsync(x => x.RoleId == roleId);
            if (entity == null) return null;

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapRole(entity);
        }

        public async Task<RoleDto?> UploadRoleIconAsync(Guid roleId, IFormFile file)
        {
            var entity = await _context.HomepageRoles.FirstOrDefaultAsync(x => x.RoleId == roleId);
            if (entity == null) return null;

            // Stores the file via the file storage provider and gets back a short,
            // public URL (e.g. https://jobportal.coinage.in/uploads/homepage/roles/xxx.png)
            // instead of persisting a raw base64 string on the entity/response.
            var upload = await _fileStorageService.UploadImageAsync(file, "homepage/roles");

            entity.IconUrl = upload.Url;
            entity.IconPublicId = upload.PublicId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapRole(entity);
        }

        private static RoleDto MapRole(HomepageRole x) => new()
        {
            RoleId = x.RoleId,
            Name = x.Name,
            IconUrl = x.IconUrl,
            JobCountOverride = x.JobCountOverride,
            DisplayOrder = x.DisplayOrder,
            IsActive = x.IsActive
        };

        // ============================================================
        // Registration Industries / Departments / Trade Categories
        // Structurally identical — routed through shared private helpers
        // keyed by a tiny accessor so we don't repeat the CRUD five times.
        // ============================================================

        // Registration Industries
        public Task<List<NamedListItemDto>> GetRegistrationIndustriesAsync() =>
            GetNamedListAsync(_context.HomepageRegistrationIndustries);

        public async Task<NamedListItemDto> CreateRegistrationIndustryAsync(CreateNamedListItemRequestDto request)
        {
            var maxOrder = await _context.HomepageRegistrationIndustries.MaxAsync(x => (int?)x.DisplayOrder) ?? 0;
            var entity = new HomepageRegistrationIndustry
            {
                RegistrationIndustryId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.HomepageRegistrationIndustries.Add(entity);
            await _context.SaveChangesAsync();
            return MapNamed(entity.RegistrationIndustryId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        public async Task<NamedListItemDto?> UpdateRegistrationIndustryAsync(Guid id, UpdateNamedListItemRequestDto request)
        {
            var entity = await _context.HomepageRegistrationIndustries.FirstOrDefaultAsync(x => x.RegistrationIndustryId == id);
            if (entity == null) return null;
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder.Value;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapNamed(entity.RegistrationIndustryId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        public async Task<bool> DeleteRegistrationIndustryAsync(Guid id)
        {
            var entity = await _context.HomepageRegistrationIndustries.FirstOrDefaultAsync(x => x.RegistrationIndustryId == id);
            if (entity == null) return false;
            _context.HomepageRegistrationIndustries.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<NamedListItemDto?> ToggleRegistrationIndustryAsync(Guid id)
        {
            var entity = await _context.HomepageRegistrationIndustries.FirstOrDefaultAsync(x => x.RegistrationIndustryId == id);
            if (entity == null) return null;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapNamed(entity.RegistrationIndustryId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        // Departments
        public Task<List<NamedListItemDto>> GetDepartmentsAsync() =>
            GetNamedListAsync(_context.HomepageDepartments);

        public async Task<NamedListItemDto> CreateDepartmentAsync(CreateNamedListItemRequestDto request)
        {
            var maxOrder = await _context.HomepageDepartments.MaxAsync(x => (int?)x.DisplayOrder) ?? 0;
            var entity = new HomepageDepartment
            {
                DepartmentId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.HomepageDepartments.Add(entity);
            await _context.SaveChangesAsync();
            return MapNamed(entity.DepartmentId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        public async Task<NamedListItemDto?> UpdateDepartmentAsync(Guid id, UpdateNamedListItemRequestDto request)
        {
            var entity = await _context.HomepageDepartments.FirstOrDefaultAsync(x => x.DepartmentId == id);
            if (entity == null) return null;
            if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
            if (request.DisplayOrder.HasValue) entity.DisplayOrder = request.DisplayOrder.Value;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapNamed(entity.DepartmentId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        public async Task<bool> DeleteDepartmentAsync(Guid id)
        {
            var entity = await _context.HomepageDepartments.FirstOrDefaultAsync(x => x.DepartmentId == id);
            if (entity == null) return false;
            _context.HomepageDepartments.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<NamedListItemDto?> ToggleDepartmentAsync(Guid id)
        {
            var entity = await _context.HomepageDepartments.FirstOrDefaultAsync(x => x.DepartmentId == id);
            if (entity == null) return null;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapNamed(entity.DepartmentId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }

        // Trade Categories
        public Task<List<NamedListItemDto>> GetTradeCategoriesAsync() => GetNamedListAsync(_context.HomepageTradeCategories);

        public async Task<NamedListItemDto> CreateTradeCategoryAsync(Guid registrationIndustryId, CreateNamedListItemRequestDto request)
        {
            // Make sure the Industry Type exists
            var industryExists = await _context.HomepageRegistrationIndustries
                .AnyAsync(x =>
                    x.RegistrationIndustryId == registrationIndustryId &&
                    x.IsActive);

            if (!industryExists)
                throw new KeyNotFoundException("Industry Type not found.");

            var name = request.Name.Trim();

            // Prevent duplicate Trade Category under the same Industry Type
            var alreadyExists = await _context.HomepageTradeCategories
                .AnyAsync(x =>
                    x.RegistrationIndustryId == registrationIndustryId &&
                    x.Name.ToLower() == name.ToLower());

            if (alreadyExists)
                throw new InvalidOperationException(
                    "A Trade Category with the same name already exists under this Industry Type.");

            // Display order should be within the selected Industry
            var maxOrder = await _context.HomepageTradeCategories
                .Where(x => x.RegistrationIndustryId == registrationIndustryId)
                .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var entity = new HomepageTradeCategory
            {
                TradeCategoryId = Guid.NewGuid(),

                RegistrationIndustryId = registrationIndustryId,

                Name = name,

                IsActive = true,

                DisplayOrder = maxOrder + 1,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageTradeCategories.Add(entity);

            await _context.SaveChangesAsync();

            return MapNamed(
                entity.TradeCategoryId,
                entity.Name,
                entity.DisplayOrder,
                entity.IsActive);
        }

        public async Task<NamedListItemDto?> UpdateTradeCategoryAsync( Guid id, UpdateNamedListItemRequestDto request)
        {
            var entity = await _context.HomepageTradeCategories
                .FirstOrDefaultAsync(x => x.TradeCategoryId == id);

            if (entity == null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();

                // Prevent duplicate Trade Category
                // under the same Industry Type
                var duplicateExists = await _context.HomepageTradeCategories
                    .AnyAsync(x =>
                        x.TradeCategoryId != id &&
                        x.RegistrationIndustryId == entity.RegistrationIndustryId &&
                        x.Name.ToLower() == name.ToLower());

                if (duplicateExists)
                    throw new InvalidOperationException(
                        "A Trade Category with the same name already exists under this Industry Type.");

                entity.Name = name;
            }

            if (request.DisplayOrder.HasValue)
                entity.DisplayOrder = request.DisplayOrder.Value;

            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapNamed(
                entity.TradeCategoryId,
                entity.Name,
                entity.DisplayOrder,
                entity.IsActive);
        }
        public async Task<bool> DeleteTradeCategoryAsync(Guid id)
        {
            var entity = await _context.HomepageTradeCategories
                .FirstOrDefaultAsync(x => x.TradeCategoryId == id);

            if (entity == null)
                return false;

            var hasSubTrades = await _context.HomepageSubTrades
                .AnyAsync(x => x.TradeCategoryId == id);

            if (hasSubTrades)
            {
                throw new InvalidOperationException(
                    "This Trade Category cannot be deleted because it has SubTrades. Delete the SubTrades first.");
            }

            _context.HomepageTradeCategories.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<NamedListItemDto?> ToggleTradeCategoryAsync(Guid id)
        {
            var entity = await _context.HomepageTradeCategories.FirstOrDefaultAsync(x => x.TradeCategoryId == id);
            if (entity == null) return null;
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapNamed(entity.TradeCategoryId, entity.Name, entity.DisplayOrder, entity.IsActive);
        }



        // ============================================================
        // Sub Trades
        // ============================================================

        public async Task<List<NamedListItemDto>> GetSubTradesAsync(Guid tradeCategoryId)
        {
            return await _context.HomepageSubTrades
                .AsNoTracking()
                .Where(x => x.TradeCategoryId == tradeCategoryId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new NamedListItemDto
                {
                    Id = x.SubTradeId,
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<NamedListItemDto> CreateSubTradeAsync(
            Guid tradeCategoryId,
            CreateNamedListItemRequestDto request)
        {
            // Make sure the parent Trade Category exists
            var tradeCategoryExists = await _context.HomepageTradeCategories
                .AnyAsync(x => x.TradeCategoryId == tradeCategoryId);

            if (!tradeCategoryExists)
                throw new KeyNotFoundException("Trade category not found.");

            var name = request.Name.Trim();

            // Prevent duplicate SubTrade under the same Trade Category
            var alreadyExists = await _context.HomepageSubTrades
                .AnyAsync(x =>
                    x.TradeCategoryId == tradeCategoryId &&
                    x.Name.ToLower() == name.ToLower());

            if (alreadyExists)
                throw new InvalidOperationException(
                    "A SubTrade with the same name already exists under this Trade Category.");

            var maxOrder = await _context.HomepageSubTrades
                .Where(x => x.TradeCategoryId == tradeCategoryId)
                .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var entity = new HomepageSubTrade
            {
                SubTradeId = Guid.NewGuid(),
                TradeCategoryId = tradeCategoryId,
                Name = name,
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HomepageSubTrades.Add(entity);

            await _context.SaveChangesAsync();

            return MapNamed(
                entity.SubTradeId,
                entity.Name,
                entity.DisplayOrder,
                entity.IsActive);
        }

        public async Task<NamedListItemDto?> UpdateSubTradeAsync(
            Guid id,
            UpdateNamedListItemRequestDto request)
        {
            var entity = await _context.HomepageSubTrades
                .FirstOrDefaultAsync(x => x.SubTradeId == id);

            if (entity == null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();

                var duplicateExists = await _context.HomepageSubTrades
                    .AnyAsync(x =>
                        x.SubTradeId != id &&
                        x.TradeCategoryId == entity.TradeCategoryId &&
                        x.Name.ToLower() == name.ToLower());

                if (duplicateExists)
                    throw new InvalidOperationException(
                        "A SubTrade with the same name already exists under this Trade Category.");

                entity.Name = name;
            }

            if (request.DisplayOrder.HasValue)
                entity.DisplayOrder = request.DisplayOrder.Value;

            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapNamed(
                entity.SubTradeId,
                entity.Name,
                entity.DisplayOrder,
                entity.IsActive);
        }

        public async Task<bool> DeleteSubTradeAsync(Guid id)
        {
            var entity = await _context.HomepageSubTrades
                .FirstOrDefaultAsync(x => x.SubTradeId == id);

            if (entity == null)
                return false;

            _context.HomepageSubTrades.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<NamedListItemDto?> ToggleSubTradeAsync(Guid id)
        {
            var entity = await _context.HomepageSubTrades
                .FirstOrDefaultAsync(x => x.SubTradeId == id);

            if (entity == null)
                return null;

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapNamed(
                entity.SubTradeId,
                entity.Name,
                entity.DisplayOrder,
                entity.IsActive);
        }
        public async Task<IndustryTradeSubTradeDto?> GetIndustryTradeSubTradesAsync(
    Guid industryId)
        {
            var industry = await _context.HomepageRegistrationIndustries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RegistrationIndustryId == industryId);

            if (industry == null)
                return null;

            var trades = await _context.HomepageTradeCategories
                .AsNoTracking()
                .Where(x => x.RegistrationIndustryId == industryId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new TradeWithSubTradesDto
                {
                    TradeCategoryId = x.TradeCategoryId,
                    Name = x.Name,

                    SubTrades = _context.HomepageSubTrades
                        .AsNoTracking()
                        .Where(st => st.TradeCategoryId == x.TradeCategoryId)
                        .OrderBy(st => st.DisplayOrder)
                        .Select(st => new SubTradeDto
                        {
                            SubTradeId = st.SubTradeId,
                            Name = st.Name
                        })
                        .ToList()
                })
                .ToListAsync();

            return new IndustryTradeSubTradeDto
            {
                IndustryId = industry.RegistrationIndustryId,
                IndustryName = industry.Name,
                Trades = trades
            };
        }
        private static async Task<List<NamedListItemDto>> GetNamedListAsync<T>(IQueryable<T> query)
            where T : class
        {
            // Local function keeps the three sections above from repeating
            // the same order/select/map boilerplate.
            if (typeof(T) == typeof(HomepageRegistrationIndustry))
            {
                var list = await ((IQueryable<HomepageRegistrationIndustry>)query)
                    .AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync();
                return list.Select(x => MapNamed(x.RegistrationIndustryId, x.Name, x.DisplayOrder, x.IsActive)).ToList();
            }
            if (typeof(T) == typeof(HomepageDepartment))
            {
                var list = await ((IQueryable<HomepageDepartment>)query)
                    .AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync();
                return list.Select(x => MapNamed(x.DepartmentId, x.Name, x.DisplayOrder, x.IsActive)).ToList();
            }
            if (typeof(T) == typeof(HomepageTradeCategory))
            {
                var list = await ((IQueryable<HomepageTradeCategory>)query)
                    .AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync();
                return list.Select(x => MapNamed(x.TradeCategoryId, x.Name, x.DisplayOrder, x.IsActive)).ToList();
            }
            return new List<NamedListItemDto>();
        }

        private static NamedListItemDto MapNamed(Guid id, string name, int displayOrder, bool isActive) => new()
        {
            Id = id,
            Name = name,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };

        // ============================================================
        // Suggestions
        // ============================================================

        public async Task<List<SuggestionDto>> GetSuggestionsAsync()
        {
            var items = await _context.HomepageSuggestions
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return items.Select(MapSuggestion).ToList();
        }

        public async Task<bool> DeleteSuggestionAsync(Guid id)
        {
            var entity = await _context.HomepageSuggestions.FirstOrDefaultAsync(x => x.SuggestionId == id);
            if (entity == null) return false;

            _context.HomepageSuggestions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SuggestionDto?> ApproveSuggestionAsync(Guid id, ReviewSuggestionRequestDto request, Guid? adminId)
        {
            var entity = await _context.HomepageSuggestions.FirstOrDefaultAsync(x => x.SuggestionId == id);
            if (entity == null) return null;

            entity.Status = HomepageSuggestionStatus.Approved;
            entity.AdminNote = request.AdminNote;
            entity.ReviewedBy = adminId;
            entity.ReviewedAt = DateTime.UtcNow;

            if (request.AddToList)
                await AddSuggestionToTargetListAsync(entity);

            await _context.SaveChangesAsync();
            return MapSuggestion(entity);
        }

        public async Task<SuggestionDto?> RejectSuggestionAsync(Guid id, ReviewSuggestionRequestDto request, Guid? adminId)
        {
            var entity = await _context.HomepageSuggestions.FirstOrDefaultAsync(x => x.SuggestionId == id);
            if (entity == null) return null;

            entity.Status = HomepageSuggestionStatus.Rejected;
            entity.AdminNote = request.AdminNote;
            entity.ReviewedBy = adminId;
            entity.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapSuggestion(entity);
        }

        /// <summary>Inserts an approved suggestion's name into the list it targets, as a new active item.</summary>
        private async Task AddSuggestionToTargetListAsync(HomepageSuggestion suggestion)
        {
            var name = suggestion.SuggestedName.Trim();

            switch (suggestion.Type)
            {
                case HomepageSuggestionType.Industry:
                    if (!await _context.HomepageIndustries.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
                        await CreateIndustryAsync(new CreateIndustryRequestDto { Name = name });
                    break;

                case HomepageSuggestionType.Location:
                    if (!await _context.HomepageLocations.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
                        await CreateLocationAsync(new CreateLocationRequestDto { Name = name });
                    break;

                case HomepageSuggestionType.Role:
                    if (!await _context.HomepageRoles.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
                        await CreateRoleAsync(new CreateRoleRequestDto { Name = name });
                    break;

                case HomepageSuggestionType.RegistrationIndustry:
                    if (!await _context.HomepageRegistrationIndustries.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
                        await CreateRegistrationIndustryAsync(new CreateNamedListItemRequestDto { Name = name });
                    break;

                case HomepageSuggestionType.Department:
                    if (!await _context.HomepageDepartments.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
                        await CreateDepartmentAsync(new CreateNamedListItemRequestDto { Name = name });
                    break;

                case HomepageSuggestionType.TradeCategory:

                    if (!suggestion.RegistrationIndustryId.HasValue)
                        throw new InvalidOperationException(
                            "Registration Industry is required for a Trade Category suggestion.");

                    var registrationIndustryId =
                        suggestion.RegistrationIndustryId.Value;

                    var tradeExists = await _context.HomepageTradeCategories
                        .AnyAsync(x =>
                            x.RegistrationIndustryId == registrationIndustryId &&
                            x.Name.ToLower() == name.ToLower());

                    if (!tradeExists)
                    {
                        await CreateTradeCategoryAsync(
                            registrationIndustryId,
                            new CreateNamedListItemRequestDto
                            {
                                Name = name
                            });
                    }

                    break;


                case HomepageSuggestionType.SubTrade:

                    if (!suggestion.TradeCategoryId.HasValue)
                        throw new InvalidOperationException(
                            "Trade Category is required for a SubTrade suggestion.");

                    var tradeCategoryId =
                        suggestion.TradeCategoryId.Value;

                    var subTradeExists = await _context.HomepageSubTrades
                        .AnyAsync(x =>
                            x.TradeCategoryId == tradeCategoryId &&
                            x.Name.ToLower() == name.ToLower());

                    if (!subTradeExists)
                    {
                        await CreateSubTradeAsync(
                            tradeCategoryId,
                            new CreateNamedListItemRequestDto
                            {
                                Name = name
                            });
                    }

                    break;
            }
        }

        private static SuggestionDto MapSuggestion(HomepageSuggestion x) => new()
        {
            SuggestionId = x.SuggestionId,
            Type = x.Type,
            SuggestedName = x.SuggestedName,
            Note = x.Note,
            SubmittedByName = x.SubmittedByName,
            SubmittedByEmail = x.SubmittedByEmail,
            Status = x.Status,
            AdminNote = x.AdminNote,
            CreatedAt = x.CreatedAt,
            ReviewedAt = x.ReviewedAt
        };

        private static string Slugify(string name) =>
            name.Trim().ToLowerInvariant().Replace(" ", "-").Replace("&", "and");
    }
}