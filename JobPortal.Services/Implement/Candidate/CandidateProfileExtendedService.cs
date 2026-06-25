

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobPortal.Services.Implement.Candidate;

public class CandidateProfileExtendedService : ICandidateProfileExtendedService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CandidateProfileExtendedService> _logger;

    public CandidateProfileExtendedService(
        AppDbContext context,
        ILogger<CandidateProfileExtendedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════
    // SECTION 3 — WORK EXPERIENCE
    // ════════════════════════════════════════════════════════

    public async Task<WorkExperienceListResponseDto> GetWorkExperienceAsync(Guid candidateId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
       .AsNoTracking()
       .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            if (profile == null)
                return WorkListFail("Candidate profile not found.");

            var entries = await _context.CandidateWorkHistories
     .AsNoTracking()
     .Where(w => w.CandidateId == candidateId)
     .OrderByDescending(w => w.StartDate)
     .Select(w => new WorkExperienceItemDto
     {
         WorkId = w.WorkId,
         JobTitle = w.JobTitle,
         CompanyName = w.CompanyName,
         WorkLocation = w.WorkLocation,
         NoticePeriod = profile.NoticePeriod,
         StartDate = w.StartDate,
         EndDate = w.EndDate,
         IsCurrent = w.IsCurrent,
         JobDescription = w.JobDescription,
         IsOffshore = w.IsOffshore
     })
     .ToListAsync();

            return new WorkExperienceListResponseDto
            {
                Success = true,
                Message = "Work experience retrieved.",
                Data = entries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWorkExperienceAsync failed for {CandidateId}", candidateId);
            return WorkListFail("Internal server error.");
        }
    }

    public async Task<WorkExperienceMutationResponseDto> AddWorkExperienceAsync(
        Guid candidateId, AddWorkExperienceRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.WorkHistories)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return WorkMutFail("Candidate profile not found.");

            // Business rule: EndDate must be provided when IsCurrent = false
            if (!request.IsCurrent && request.EndDate == null)
                return WorkMutFail("End date is required when 'Currently working here' is unchecked.");

            // Business rule: EndDate must not precede StartDate
            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
                return WorkMutFail("End date cannot be earlier than start date.");

            // If candidate marks a new entry as current, clear previous current flag
            if (request.IsCurrent)
            {
                var previousCurrent = await _context.CandidateWorkHistories
                    .Where(w => w.CandidateId == candidateId && w.IsCurrent)
                    .ToListAsync();
                foreach (var prev in previousCurrent)
                    prev.IsCurrent = false;
            }

            // Update candidate profile
            profile.NoticePeriod = request.NoticePeriod;

            var entry = new CandidateWorkHistory
            {
                WorkId = Guid.NewGuid(),
                CandidateId = candidateId,
                JobTitle = request.JobTitle,
                CompanyName = request.CompanyName,
                WorkLocation = request.WorkLocation,
                StartDate = request.StartDate,
                EndDate = request.IsCurrent ? null : request.EndDate,
                IsCurrent = request.IsCurrent,
                JobDescription = request.JobDescription,
                IsOffshore = request.IsOffshore
            };

         

            _context.CandidateWorkHistories.Add(entry);

            // Recalculate profile completion
            profile.WorkHistories.Add(entry);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkExperienceMutationResponseDto
            {
                Success = true,
                Message = "Work experience added.",
                WorkId = entry.WorkId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddWorkExperienceAsync failed for {CandidateId}", candidateId);
            return WorkMutFail("Internal server error.");
        }
    }

    public async Task<WorkExperienceMutationResponseDto> UpdateWorkExperienceAsync(
        Guid candidateId, Guid workId, UpdateWorkExperienceRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.WorkHistories)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return WorkMutFail("Candidate profile not found.");

            var entry = profile.WorkHistories.FirstOrDefault(w => w.WorkId == workId);
            if (entry == null)
                return WorkMutFail("Work experience entry not found.");

            if (!request.IsCurrent && request.EndDate == null)
                return WorkMutFail("End date is required when 'Currently working here' is unchecked.");

            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
                return WorkMutFail("End date cannot be earlier than start date.");

            // Clear IsCurrent flag on other entries if this becomes current
            if (request.IsCurrent && !entry.IsCurrent)
            {
                var others = profile.WorkHistories.Where(w => w.WorkId != workId && w.IsCurrent);
                foreach (var o in others)
                    o.IsCurrent = false;
            }

            entry.JobTitle = request.JobTitle;
            entry.CompanyName = request.CompanyName;
            entry.WorkLocation = request.WorkLocation;
            profile.NoticePeriod = request.NoticePeriod;
            entry.StartDate = request.StartDate;
            entry.EndDate = request.IsCurrent ? null : request.EndDate;
            entry.IsCurrent = request.IsCurrent;
            entry.JobDescription = request.JobDescription;
            entry.IsOffshore = request.IsOffshore;

            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkExperienceMutationResponseDto
            {
                Success = true,
                Message = "Work experience updated.",
                WorkId = entry.WorkId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateWorkExperienceAsync failed for {CandidateId}/{WorkId}", candidateId, workId);
            return WorkMutFail("Internal server error.");
        }
    }

    public async Task<WorkExperienceMutationResponseDto> DeleteWorkExperienceAsync(
        Guid candidateId, Guid workId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.WorkHistories)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return WorkMutFail("Candidate profile not found.");

            var entry = profile.WorkHistories.FirstOrDefault(w => w.WorkId == workId);
            if (entry == null)
                return WorkMutFail("Work experience entry not found.");

            _context.CandidateWorkHistories.Remove(entry);
            profile.WorkHistories.Remove(entry);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkExperienceMutationResponseDto
            {
                Success = true,
                Message = "Work experience removed.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteWorkExperienceAsync failed for {CandidateId}/{WorkId}", candidateId, workId);
            return WorkMutFail("Internal server error.");
        }
    }

  
    public async Task<EducationListResponseDto> GetEducationAsync(Guid candidateId)
    {
        try
        {
            var exists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!exists)
                return EduListFail("Candidate profile not found.");
            var entries = await _context.CandidateEducations
       .AsNoTracking()
       .Where(e => e.CandidateId == candidateId)
       .OrderByDescending(e => e.PassoutYear)
       .Select(e => new EducationItemDto
       {
           EducationId = e.EducationId,

           QualificationDegree = e.EducationLevel,

           InstituteName = e.InstituteName,

           PassoutYear = e.PassoutYear,

           YearDetails = e.YearDetails,

           CertificateUrl = e.CertificateUrl,

           CertificateNumber = e.CertificateNumber,

           IsAiVerified = e.IsAiVerified
       })
       .ToListAsync();

            return new EducationListResponseDto
            {
                Success = true,
                Message = "Education details retrieved.",
                Data = entries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEducationAsync failed for {CandidateId}", candidateId);
            return EduListFail("Internal server error.");
        }
    }

    public async Task<EducationMutationResponseDto> AddEducationAsync(
        Guid candidateId, AddEducationRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return EduMutFail("Candidate profile not found.");

            var entry = new CandidateEducation
            {
                EducationId = Guid.NewGuid(),
                CandidateId = candidateId,
                EducationLevel = request.QualificationDegree,
                InstituteName = request.InstituteName,
                YearDetails = request.YearDetails,
                IsAiVerified = request.IsAiVerified,
                PassoutYear = request.PassoutYear,
                CertificateNumber = request.CertificateNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.CandidateEducations.Add(entry);
            profile.Educations.Add(entry);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new EducationMutationResponseDto
            {
                Success = true,
                Message = "Education qualification added.",
                EducationId = entry.EducationId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddEducationAsync failed for {CandidateId}", candidateId);
            return EduMutFail("Internal server error.");
        }
    }

    public async Task<EducationMutationResponseDto> UpdateEducationAsync(
        Guid candidateId, Guid educationId, UpdateEducationRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return EduMutFail("Candidate profile not found.");

            var entry = profile.Educations.FirstOrDefault(e => e.EducationId == educationId);
            if (entry == null)
                return EduMutFail("Education entry not found.");

            entry.EducationLevel = request.QualificationDegree;
            entry.InstituteName = request.InstituteName;
            entry.YearDetails = request.YearDetails;
            entry.IsAiVerified = request.IsAiVerified;
            entry.PassoutYear = request.PassoutYear;
            entry.CertificateNumber = request.CertificateNumber;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new EducationMutationResponseDto
            {
                Success = true,
                Message = "Education qualification updated.",
                EducationId = entry.EducationId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateEducationAsync failed for {CandidateId}/{EducationId}", candidateId, educationId);
            return EduMutFail("Internal server error.");
        }
    }

    public async Task<EducationMutationResponseDto> DeleteEducationAsync(
        Guid candidateId, Guid educationId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Educations)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return EduMutFail("Candidate profile not found.");

            var entry = profile.Educations.FirstOrDefault(e => e.EducationId == educationId);
            if (entry == null)
                return EduMutFail("Education entry not found.");

            _context.CandidateEducations.Remove(entry);
            profile.Educations.Remove(entry);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new EducationMutationResponseDto
            {
                Success = true,
                Message = "Education qualification removed.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteEducationAsync failed for {CandidateId}/{EducationId}", candidateId, educationId);
            return EduMutFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════════════
    // SECTION 5 — SKILLS
    // ════════════════════════════════════════════════════════

    public async Task<SkillsListResponseDto> GetSkillsAsync(Guid candidateId)
    {
        try
        {
            var exists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!exists)
                return new SkillsListResponseDto { Success = false, Message = "Candidate profile not found." };

            var skills = await _context.CandidateSkills
                .Where(s => s.CandidateId == candidateId && s.SkillType == "Skill")
                .OrderBy(s => s.SkillName)
                .Select(s => new SkillItemDto
                {
                    SkillId = s.SkillId,
                    SkillName = s.SkillName,
                    SkillType = s.SkillType,
                    ProficiencyLevel = s.SkillRole,           // "Beginner"|"Intermediate"|"Expert"
                    YearsOfExperience = s.YearsOfExperience
                })
                .ToListAsync();

            return new SkillsListResponseDto
            {
                Success = true,
                Message = "Skills retrieved.",
                Data = new SkillsListData
                {
                    Skills = skills,
                    TotalCount = skills.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSkillsAsync failed for {CandidateId}", candidateId);
            return new SkillsListResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<SkillMutationResponseDto> AddSkillAsync(
        Guid candidateId, AddSkillRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return SkillMutFail("Candidate profile not found.");

            // Prevent duplicate skill name (case-insensitive)
            var duplicate = profile.Skills.Any(s =>
                s.SkillType == "Skill" &&
                string.Equals(s.SkillName, request.SkillName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                return SkillMutFail($"Skill '{request.SkillName}' has already been added.");

            var skill = new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = request.SkillName,
                SkillType = "Skill",
                SkillRole = request.ProficiencyLevel,
                YearsOfExperience = request.YearsOfExperience
            };

            _context.CandidateSkills.Add(skill);
            profile.Skills.Add(skill);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SkillMutationResponseDto
            {
                Success = true,
                Message = "Skill added.",
                SkillId = skill.SkillId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddSkillAsync failed for {CandidateId}", candidateId);
            return SkillMutFail("Internal server error.");
        }
    }

    public async Task<SkillMutationResponseDto> UpdateSkillAsync(
        Guid candidateId, Guid skillId, UpdateSkillRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return SkillMutFail("Candidate profile not found.");

            var skill = profile.Skills.FirstOrDefault(s => s.SkillId == skillId && s.SkillType == "Skill");
            if (skill == null)
                return SkillMutFail("Skill not found.");

            skill.SkillName = request.SkillName;
            skill.SkillRole = request.ProficiencyLevel;
            skill.YearsOfExperience = request.YearsOfExperience;

            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SkillMutationResponseDto
            {
                Success = true,
                Message = "Skill updated.",
                SkillId = skill.SkillId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSkillAsync failed for {CandidateId}/{SkillId}", candidateId, skillId);
            return SkillMutFail("Internal server error.");
        }
    }

    public async Task<SkillMutationResponseDto> DeleteSkillAsync(
        Guid candidateId, Guid skillId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return SkillMutFail("Candidate profile not found.");

            var skill = profile.Skills.FirstOrDefault(s => s.SkillId == skillId && s.SkillType == "Skill");
            if (skill == null)
                return SkillMutFail("Skill not found.");

            _context.CandidateSkills.Remove(skill);
            profile.Skills.Remove(skill);
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SkillMutationResponseDto
            {
                Success = true,
                Message = "Skill removed.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteSkillAsync failed for {CandidateId}/{SkillId}", candidateId, skillId);
            return SkillMutFail("Internal server error.");
        }
    }

    public async Task<BulkSaveSkillsResponseDto> BulkSaveSkillsAsync(
        Guid candidateId, BulkSaveSkillsRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return new BulkSaveSkillsResponseDto { Success = false, Message = "Candidate profile not found." };

            // Remove existing skills only (preserve languages)
            var existingSkills = profile.Skills.Where(s => s.SkillType == "Skill").ToList();
            _context.CandidateSkills.RemoveRange(existingSkills);
            foreach (var s in existingSkills)
                profile.Skills.Remove(s);

            // Add the incoming set
            var newSkills = request.Skills.Select(dto => new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = dto.SkillName,
                SkillType = "Skill",
                SkillRole = dto.ProficiencyLevel,
                YearsOfExperience = dto.YearsOfExperience
            }).ToList();

            _context.CandidateSkills.AddRange(newSkills);
            foreach (var s in newSkills)
                profile.Skills.Add(s);

            profile.ProfileCompletionPct = CalculateCompletionPct(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new BulkSaveSkillsResponseDto
            {
                Success = true,
                Message = $"{newSkills.Count} skill(s) saved.",
                SavedCount = newSkills.Count,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkSaveSkillsAsync failed for {CandidateId}", candidateId);
            return new BulkSaveSkillsResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    // ════════════════════════════════════════════════════════
    // SECTION 6 — LANGUAGES
    // ════════════════════════════════════════════════════════
    //
    // Languages are stored in CandidateSkill with SkillType = "Language".
    // The SkillRole column stores proficiency level ("Native","Professional","Conversational","Basic").
    // Reading/Writing/Speaking flags are packed into YearsOfExperience as a 3-bit bitmask:
    //   bit 0 (1) = CanRead
    //   bit 1 (2) = CanWrite
    //   bit 2 (4) = CanSpeak
    // This avoids a schema change; a dedicated CandidateLanguage entity is the cleaner
    // long-term solution but requires a new migration.
    // ════════════════════════════════════════════════════════

    public async Task<LanguagesListResponseDto> GetLanguagesAsync(Guid candidateId)
    {
        try
        {
            var exists = await _context.CandidateProfiles
                .AnyAsync(p => p.CandidateId == candidateId);
            if (!exists)
                return new LanguagesListResponseDto { Success = false, Message = "Candidate profile not found." };

            var langs = await _context.CandidateSkills
                .Where(s => s.CandidateId == candidateId && s.SkillType == "Language")
                .OrderBy(s => s.SkillName)
                .ToListAsync();

            var data = langs.Select(s =>
            {
                var flags = s.YearsOfExperience ?? 0;
                return new LanguageItemDto
                {
                    SkillId = s.SkillId,
                    LanguageName = s.SkillName,
                    ProficiencyLevel = s.SkillRole ?? "Conversational",
                    CanRead = s.CanRead ?? false,
                    CanWrite = s.CanWrite ?? false,
                    CanSpeak = s.CanSpeak ?? false
                };
            }).ToList();

            return new LanguagesListResponseDto
            {
                Success = true,
                Message = "Language preferences retrieved.",
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLanguagesAsync failed for {CandidateId}", candidateId);
            return new LanguagesListResponseDto { Success = false, Message = "Internal server error." };
        }
    }

    public async Task<LanguageMutationResponseDto> AddLanguageAsync(
        Guid candidateId, AddLanguageRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return LangMutFail("Candidate profile not found.");

            // Prevent duplicate language
            var duplicate = profile.Skills.Any(s =>
                s.SkillType == "Language" &&
                string.Equals(s.SkillName, request.LanguageName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                return LangMutFail($"Language '{request.LanguageName}' has already been added.");



            var lang = new CandidateSkill
            {
                SkillId = Guid.NewGuid(),
                CandidateId = candidateId,
                SkillName = request.LanguageName,
                SkillType = "Language",
                SkillRole = request.ProficiencyLevel,

                CanRead = request.CanRead,
                CanWrite = request.CanWrite,
                CanSpeak = request.CanSpeak
            };

            _context.CandidateSkills.Add(lang);
            profile.Skills.Add(lang);
            profile.UpdatedAt = DateTime.UtcNow;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);

            await _context.SaveChangesAsync();

            return new LanguageMutationResponseDto
            {
                Success = true,
                Message = "Language added.",
                SkillId = lang.SkillId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddLanguageAsync failed for {CandidateId}", candidateId);
            return LangMutFail("Internal server error.");
        }
    }

    public async Task<LanguageMutationResponseDto> UpdateLanguageAsync(
        Guid candidateId, Guid skillId, UpdateLanguageRequestDto request)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return LangMutFail("Candidate profile not found.");

            var lang = profile.Skills.FirstOrDefault(s => s.SkillId == skillId && s.SkillType == "Language");
            if (lang == null)
                return LangMutFail("Language entry not found.");

            lang.SkillName = request.LanguageName;
            lang.SkillRole = request.ProficiencyLevel;
            lang.CanRead = request.CanRead;
            lang.CanWrite = request.CanWrite;
            lang.CanSpeak = request.CanSpeak;

            profile.UpdatedAt = DateTime.UtcNow;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);

            await _context.SaveChangesAsync();

            return new LanguageMutationResponseDto
            {
                Success = true,
                Message = "Language updated.",
                SkillId = lang.SkillId,
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateLanguageAsync failed for {CandidateId}/{SkillId}", candidateId, skillId);
            return LangMutFail("Internal server error.");
        }
    }

    public async Task<LanguageMutationResponseDto> DeleteLanguageAsync(
        Guid candidateId, Guid skillId)
    {
        try
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);
            if (profile == null)
                return LangMutFail("Candidate profile not found.");

            var lang = profile.Skills.FirstOrDefault(s => s.SkillId == skillId && s.SkillType == "Language");
            if (lang == null)
                return LangMutFail("Language entry not found.");

            _context.CandidateSkills.Remove(lang);
            profile.Skills.Remove(lang);
            profile.UpdatedAt = DateTime.UtcNow;
            profile.ProfileCompletionPct = CalculateCompletionPct(profile);

            await _context.SaveChangesAsync();

            return new LanguageMutationResponseDto
            {
                Success = true,
                Message = "Language removed.",
                ProfileCompletionPct = profile.ProfileCompletionPct
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteLanguageAsync failed for {CandidateId}/{SkillId}", candidateId, skillId);
            return LangMutFail("Internal server error.");
        }
    }

    // ════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Lightweight completion recalculation using already-loaded navigation collections.
    /// Keeps parity with CandidateProfileService.CalculateCompletionPct.
    /// </summary>
    private static byte CalculateCompletionPct(CandidateProfile p)
    {
        int score = 0;
        if (!string.IsNullOrWhiteSpace(p.FullName)) score += 10;
        if (!string.IsNullOrWhiteSpace(p.ProfilePhotoUrl)) score += 15;
        if (p.DateOfBirth.HasValue) score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentCity)) score += 5;
        if (!string.IsNullOrWhiteSpace(p.CurrentState)) score += 5;
        if (p.TotalExperienceYears > 0) score += 10;
        if (p.Cvs?.Any(c => c.CvFileUrl != null) == true) score += 20;
        if (p.Educations?.Any() == true) score += 10;
        if (p.WorkHistories?.Any() == true) score += 10;
        if (p.Skills?.Any(s => s.SkillType == "Skill") == true) score += 10;
        return (byte)Math.Min(score, 100);
    }

    /// <summary>Packs Read/Write/Speak booleans into a 3-bit byte.</summary>
 

    // ── Fail helpers ─────────────────────────────────────────

    private static WorkExperienceListResponseDto WorkListFail(string msg)
        => new() { Success = false, Message = msg };

    private static WorkExperienceMutationResponseDto WorkMutFail(string msg)
        => new() { Success = false, Message = msg };

    private static EducationListResponseDto EduListFail(string msg)
        => new() { Success = false, Message = msg };

    private static EducationMutationResponseDto EduMutFail(string msg)
        => new() { Success = false, Message = msg };

    private static SkillMutationResponseDto SkillMutFail(string msg)
        => new() { Success = false, Message = msg };

    private static LanguageMutationResponseDto LangMutFail(string msg)
        => new() { Success = false, Message = msg };
}