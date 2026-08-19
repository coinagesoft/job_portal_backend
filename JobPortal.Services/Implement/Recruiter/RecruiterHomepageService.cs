// ============================================================
//  JobPortal.Services/Implement/Recruiter/RecruiterHomepageService.cs
//
//  Read side: admin-managed lists shown as dropdowns to recruiters —
//  Industry Type (Employer Registration Step 1) and Trade/Role +
//  Department (Job Posting form).
//
//  Write side: "Other" suggestions from either of those forms. Stored in
//  the same HomepageSuggestion table the admin Suggestions inbox reads
//  from (JobPortal.API/Controllers/Admin/AdminHomepageController.cs), so
//  an approval there inserts straight into the matching dropdown — no
//  separate recruiter-only suggestion pipeline needed.
// ============================================================

using JobPortal.Application.DTOs.Recruiter.Homepage;
using JobPortal.Domain.Entities.Homepage;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter;

public class RecruiterHomepageService : IRecruiterHomepageService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RecruiterHomepageService> _logger;

    // Maps the friendly "Field" a controller allows through to the shared
    // HomepageSuggestionType enum used by the admin Suggestions inbox.
    private static readonly Dictionary<string, HomepageSuggestionType> FieldMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Industry"] = HomepageSuggestionType.RegistrationIndustry,
            ["TradeRole"] = HomepageSuggestionType.TradeCategory,
            ["Department"] = HomepageSuggestionType.Department,
        };

    public RecruiterHomepageService(AppDbContext context, ILogger<RecruiterHomepageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Registration — Step 1 (Industry Type) ───────────────────────

    public async Task<RecruiterIndustriesResponseDto> GetRegistrationIndustriesAsync()
    {
        try
        {
            var industries = await _context.HomepageRegistrationIndustries
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RecruiterDropdownOptionDto { Id = x.RegistrationIndustryId, Name = x.Name })
                .ToListAsync();

            return new RecruiterIndustriesResponseDto
            {
                Success = true,
                Message = "Industry options loaded.",
                Industries = industries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecruiterHomepageService.GetRegistrationIndustriesAsync failed.");
            return new RecruiterIndustriesResponseDto
            {
                Success = false,
                Message = "An error occurred while loading industry options."
            };
        }
    }

    // ── Job Posting (Trade/Role, Department) ────────────────────────

    public async Task<RecruiterJobPostingDropdownsResponseDto> GetJobPostingDropdownsAsync()
    {
        try
        {
            var tradeRoles = await _context.HomepageTradeCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RecruiterDropdownOptionDto { Id = x.TradeCategoryId, Name = x.Name })
                .ToListAsync();

            var departments = await _context.HomepageDepartments
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RecruiterDropdownOptionDto { Id = x.DepartmentId, Name = x.Name })
                .ToListAsync();

            return new RecruiterJobPostingDropdownsResponseDto
            {
                Success = true,
                Message = "Dropdown options loaded.",
                TradeRoles = tradeRoles,
                Departments = departments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecruiterHomepageService.GetJobPostingDropdownsAsync failed.");
            return new RecruiterJobPostingDropdownsResponseDto
            {
                Success = false,
                Message = "An error occurred while loading dropdown options."
            };
        }
    }

    // ── Suggestions (shared, field-restricted per caller) ───────────

    public async Task<RecruiterSuggestionResponseDto> SubmitSuggestionAsync(
        RecruiterSuggestionRequestDto request,
        Guid? submittedByUserId,
        params string[] allowedFields)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SuggestedName))
                return new RecruiterSuggestionResponseDto { Success = false, Message = "SuggestedName is required." };

            var field = request.Field?.Trim() ?? string.Empty;

            var isAllowed = allowedFields.Any(f => string.Equals(f, field, StringComparison.OrdinalIgnoreCase));
            if (!isAllowed || !FieldMap.TryGetValue(field, out var type))
            {
                return new RecruiterSuggestionResponseDto
                {
                    Success = false,
                    Message = $"Field must be one of: {string.Join(", ", allowedFields)}."
                };
            }

            var suggestedName = request.SuggestedName.Trim();

            // Skip a duplicate pending suggestion for the same field + name
            // instead of piling up near-identical rows in the admin inbox.
            var alreadyPending = await _context.HomepageSuggestions.AnyAsync(s =>
                s.Type == type &&
                s.Status == HomepageSuggestionStatus.Pending &&
                s.SuggestedName.ToLower() == suggestedName.ToLower());

            if (alreadyPending)
            {
                return new RecruiterSuggestionResponseDto
                {
                    Success = true,
                    Message = "This has already been suggested and is pending admin review."
                };
            }

            var entity = new HomepageSuggestion
            {
                SuggestionId = Guid.NewGuid(),
                Type = type,
                SuggestedName = suggestedName,
                Note = request.Note,
                SubmittedByUserId = submittedByUserId,
                SubmittedByName = request.SubmittedByName,
                SubmittedByEmail = request.SubmittedByEmail,
                Status = HomepageSuggestionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.HomepageSuggestions.Add(entity);
            await _context.SaveChangesAsync();

            return new RecruiterSuggestionResponseDto
            {
                Success = true,
                Message = "Thanks! Your suggestion has been submitted for review.",
                SuggestionId = entity.SuggestionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecruiterHomepageService.SubmitSuggestionAsync failed.");
            return new RecruiterSuggestionResponseDto
            {
                Success = false,
                Message = "An error occurred while submitting your suggestion."
            };
        }
    }
}