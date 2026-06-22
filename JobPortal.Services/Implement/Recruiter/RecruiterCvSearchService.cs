using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCvSearchService : IRecruiterCvSearchService
    {
        private readonly AppDbContext _context;

        public RecruiterCvSearchService(
            AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Dashboard
        // =====================================================

        public async Task<CvSearchDashboardDto> GetDashboardAsync(
            Guid employerId)
        {
            var candidates =
                await _context.CandidateProfiles
                    .AsNoTracking()
                    .ToListAsync();

            return new CvSearchDashboardDto
            {
                TotalCandidates = candidates.Count,

                BandA = candidates.Count(x =>
                    x.Band == "A"),

                BandB = candidates.Count(x =>
                    x.Band == "B"),

                BandC = candidates.Count(x =>
                    x.Band == "C")
            };
        }

        // =====================================================
        // Search Candidates
        // =====================================================

        public async Task<CvSearchResponseDto> SearchCandidatesAsync(
            Guid employerId,
            CvSearchRequestDto request)
        {
            var query =
                _context.CandidateProfiles
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Skills)
                    .Include(x => x.Cvs)
                    .AsQueryable();

            // -----------------------------------------
            // Keyword
            // -----------------------------------------

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword =
                    request.Keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.FullName.ToLower().Contains(keyword)
                    ||
                    (x.PrimaryTrade != null &&
                     x.PrimaryTrade.ToLower().Contains(keyword))
                    ||
                    x.Skills.Any(s =>
                        s.SkillName.ToLower()
                            .Contains(keyword)));
            }

            // -----------------------------------------
            // Trade
            // -----------------------------------------

            if (!string.IsNullOrWhiteSpace(
                request.TradeCategory))
            {
                query = query.Where(x =>
                    x.PrimaryTrade ==
                    request.TradeCategory);
            }

            // -----------------------------------------
            // Experience
            // -----------------------------------------

            if (request.MinExperience.HasValue)
            {
                query = query.Where(x =>
                    x.TotalExperienceYears >=
                    request.MinExperience.Value);
            }

            if (request.MaxExperience.HasValue)
            {
                query = query.Where(x =>
                    x.TotalExperienceYears <=
                    request.MaxExperience.Value);
            }

            // -----------------------------------------
            // Location
            // -----------------------------------------

            if (!string.IsNullOrWhiteSpace(
                request.Location))
            {
                var location =
                    request.Location.Trim().ToLower();

                query = query.Where(x =>
                    (x.CurrentCity != null &&
                     x.CurrentCity.ToLower()
                         .Contains(location))
                    ||
                    (x.CurrentState != null &&
                     x.CurrentState.ToLower()
                         .Contains(location)));
            }

            // -----------------------------------------
            // Availability
            // -----------------------------------------

            if (!string.IsNullOrWhiteSpace(
                request.AvailabilityStatus))
            {
                query = query.Where(x =>
                    x.AvailabilityStatus ==
                    request.AvailabilityStatus);
            }

            // -----------------------------------------
            // ITI Certified
            // -----------------------------------------

            if (request.ItiCertifiedOnly)
            {
                query = query.Where(x =>
                    x.ItiCertified);
            }
            // -----------------------------------------
            // Passport Valid
            // -----------------------------------------

            if (request.PassportValidOnly)
            {
                query = query.Where(x =>
                    _context.KycVerifications.Any(k =>
                        k.CandidateId == x.CandidateId &&
                        k.IdType == "Passport"));
            }

            // -----------------------------------------
            // Unlocked Profiles Only
            // -----------------------------------------

            if (request.UnlockedProfilesOnly)
            {
                query = query.Where(x =>
                    _context.EmployerCandidateAccesses.Any(a =>
                        a.EmployerId == employerId &&
                        a.CandidateId == x.CandidateId &&
                        a.IsActive));
            }

            // -----------------------------------------
            // Sorting
            // -----------------------------------------

            query = request.SortBy switch
            {
                "Experience" =>
                    query.OrderByDescending(x =>
                        x.TotalExperienceYears),

                "Newest" =>
                    query.OrderByDescending(x =>
                        x.CreatedAt),

                _ =>
                    query.OrderByDescending(x =>
                        x.AiMatchScore ?? 0)
            };

            // -----------------------------------------
            // Total Count
            // -----------------------------------------

            var totalCandidates =
                await query.CountAsync();

            // -----------------------------------------
            // Paging
            // -----------------------------------------

            var candidates =
                await query
                    .Skip(
                        (request.PageNumber - 1)
                        * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

            // -----------------------------------------
            // Map Cards
            // -----------------------------------------

            var candidateCards =
                new List<CvSearchCandidateCardDto>();

            foreach (var candidate in candidates)
            {
                var isUnlocked =
                    await _context
                        .EmployerCandidateAccesses
                        .AnyAsync(x =>
                            x.EmployerId == employerId &&
                            x.CandidateId == candidate.CandidateId &&
                            x.IsActive);

                var passportValid =
                    await _context
                        .KycVerifications
                        .AnyAsync(x =>
                            x.CandidateId == candidate.CandidateId &&
                            x.IdType == "Passport");

                var skillNames =
                    candidate.Skills
                        .Select(x => x.SkillName)
                        .Distinct()
                        .Take(10)
                        .ToList();

                candidateCards.Add(
                    new CvSearchCandidateCardDto
                    {
                        CandidateId =
                            candidate.CandidateId,

                        FullName =
                            candidate.FullName,

                        ProfilePhotoUrl =
                            candidate.ProfilePhotoUrl,

                        PrimaryTrade =
                            candidate.PrimaryTrade,

                        ExperienceYears =
                            candidate.TotalExperienceYears,

                        CurrentCity =
                            candidate.CurrentCity,

                        CurrentState =
                            candidate.CurrentState,

                        AvailabilityStatus =
                            candidate.AvailabilityStatus,

                        KeywordMatchPercentage =
                            candidate.AiMatchScore ?? 0,

                        Band =
                            candidate.Band,

                        IsItiCertified =
                            candidate.ItiCertified,

                        IsPassportValid =
                            passportValid,

                        IsKycVerified =
                            candidate.User.KycStatus
                                .ToString()
                                .Equals(
                                    "Verified",
                                    StringComparison.OrdinalIgnoreCase),

                        IsUnlocked =
                            isUnlocked,

                        CanDownloadCv =
                            isUnlocked,

                        UnlockCredits =
                            candidate.Band switch
                            {
                                "A" => 1,
                                "B" => 2,
                                "C" => 3,
                                _ => 2
                            },

                        Skills =
                            skillNames
                    });
            }

            return new CvSearchResponseDto
            {
                TotalCandidates =
                    totalCandidates,

                PageNumber =
                    request.PageNumber,

                PageSize =
                    request.PageSize,

                Candidates =
                    candidateCards
            };
        }

        // =====================================================
        // Candidate Preview
        // =====================================================

        public async Task<CandidatePreviewDto?>
            GetCandidatePreviewAsync(
                Guid employerId,
                Guid candidateId)
        {
            var candidate =
                await _context.CandidateProfiles
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Skills)
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == candidateId);

            if (candidate == null)
            {
                return null;
            }

            var passportValid =
                await _context.KycVerifications
                    .AnyAsync(x =>
                        x.CandidateId == candidateId &&
                        x.IdType == "Passport");

            return new CandidatePreviewDto
            {
                CandidateId =
                    candidate.CandidateId,

                FullName =
                    candidate.FullName,

                ProfilePhotoUrl =
                    candidate.ProfilePhotoUrl,

                PrimaryTrade =
                    candidate.PrimaryTrade,

                ExperienceYears =
                    candidate.TotalExperienceYears,

                CurrentCity =
                    candidate.CurrentCity,

                CurrentState =
                    candidate.CurrentState,

                ProfessionalSummary =
                    candidate.ProfessionalSummary,

                AvailabilityStatus =
                    candidate.AvailabilityStatus,

                IsItiCertified =
                    candidate.ItiCertified,

                IsPassportValid =
                    passportValid,

                IsKycVerified =
                    candidate.User.KycStatus
                        .ToString()
                        .Equals(
                            "Verified",
                            StringComparison.OrdinalIgnoreCase),

                Skills =
                    candidate.Skills
                        .Select(x => x.SkillName)
                        .Distinct()
                        .ToList()
            };
        }

        // =====================================================
        // Filter Options
        // =====================================================

        public async Task<CvSearchFilterOptionsDto>
            GetFilterOptionsAsync()
        {
            var trades =
                await _context.CandidateProfiles
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.PrimaryTrade))
                    .Select(x => x.PrimaryTrade!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            var cities =
                await _context.CandidateProfiles
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.CurrentCity))
                    .Select(x => x.CurrentCity!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            var availabilityStatuses =
                await _context.CandidateProfiles
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.AvailabilityStatus))
                    .Select(x => x.AvailabilityStatus)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            return new CvSearchFilterOptionsDto
            {
                TradeCategories =
                    trades,

                Locations =
                    cities,

                AvailabilityStatuses =
                    availabilityStatuses
            };
        }

        // =====================================================
        // Unlocked Candidates
        // =====================================================

        public async Task<List<CvSearchCandidateCardDto>>
            GetUnlockedCandidatesAsync(
                Guid employerId)
        {
            var unlockedCandidates =
                await _context.EmployerCandidateAccesses
                    .AsNoTracking()
                    .Where(x =>
                        x.EmployerId == employerId &&
                        x.IsActive)
                    .Select(x => x.CandidateId)
                    .ToListAsync();

            var candidates =
                await _context.CandidateProfiles
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Skills)
                    .Where(x =>
                        unlockedCandidates.Contains(
                            x.CandidateId))
                    .ToListAsync();

            var result =
                new List<CvSearchCandidateCardDto>();

            foreach (var candidate in candidates)
            {
                var passportValid =
                    await _context.KycVerifications
                        .AnyAsync(x =>
                            x.CandidateId ==
                            candidate.CandidateId &&
                            x.IdType == "Passport");

                result.Add(
                    new CvSearchCandidateCardDto
                    {
                        CandidateId =
                            candidate.CandidateId,

                        FullName =
                            candidate.FullName,

                        ProfilePhotoUrl =
                            candidate.ProfilePhotoUrl,

                        PrimaryTrade =
                            candidate.PrimaryTrade,

                        ExperienceYears =
                            candidate.TotalExperienceYears,

                        CurrentCity =
                            candidate.CurrentCity,

                        CurrentState =
                            candidate.CurrentState,

                        AvailabilityStatus =
                            candidate.AvailabilityStatus,

                        KeywordMatchPercentage =
                            candidate.AiMatchScore ?? 0,

                        Band =
                            candidate.Band,

                        IsItiCertified =
                            candidate.ItiCertified,

                        IsPassportValid =
                            passportValid,

                        IsKycVerified =
                            candidate.User.KycStatus
                                .ToString()
                                .Equals(
                                    "Verified",
                                    StringComparison.OrdinalIgnoreCase),

                        IsUnlocked = true,

                        CanDownloadCv = true,

                        UnlockCredits =
                            candidate.Band switch
                            {
                                "A" => 1,
                                "B" => 2,
                                "C" => 3,
                                _ => 2
                            },

                        Skills =
                            candidate.Skills
                                .Select(x => x.SkillName)
                                .Distinct()
                                .Take(10)
                                .ToList()
                    });
            }

            return result;
        }
    }
}