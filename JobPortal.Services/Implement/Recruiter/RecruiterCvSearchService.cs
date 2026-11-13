using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Application.DTOs.Recruiter.CandidateProfile;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using JobPortal.Services.IImplement.AI;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCvSearchService : IRecruiterCvSearchService
    {
        private readonly AppDbContext _context;
        private readonly IJobMatchingService _jobMatchingService;
        private readonly ICreditConfigurationService _creditConfigurationService;

        public RecruiterCvSearchService(
      AppDbContext context,
      IJobMatchingService jobMatchingService,
      ICreditConfigurationService creditConfigurationService)
        {
            _context = context;
            _jobMatchingService = jobMatchingService;
            _creditConfigurationService = creditConfigurationService;
        }

        // Unlock cost is decided by the candidate's main trade -> Band (tier)
        // mapping managed in Admin > Homepage Management. This is the same
        // lookup CreditWalletService.UnlockCandidateAsync uses to deduct
        // credits, so the figure shown on a card always equals the charge.
        // Key = trimmed, lower-cased trade name. A trade with no active
        // band is simply absent (the card shows 0 = "not priced").
        private async Task<Dictionary<string, int>> GetTradeUnlockCostMapAsync()
        {
            var rows = await _context.TradeCreditMappings
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.SubTradeId == null &&
                    x.TradeCategory.IsActive &&
                    x.Tier.IsActive)
                .Select(x => new
                {
                    x.TradeCategory.Name,
                    x.Tier.UnlockCredits
                })
                .ToListAsync();

            var map = new Dictionary<string, int>();

            foreach (var row in rows)
            {
                var key = (row.Name ?? string.Empty).Trim().ToLower();

                if (key.Length > 0 && !map.ContainsKey(key))
                    map[key] = row.UnlockCredits;
            }

            return map;
        }

        private static int UnlockCostFor(
            Dictionary<string, int> map,
            string? primaryTrade)
        {
            var key = (primaryTrade ?? string.Empty).Trim().ToLower();

            return key.Length > 0 && map.TryGetValue(key, out var credits)
                ? credits
                : 0;
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
                    .Where(x => !x.User.IsDeleted)
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
        // Keyword helpers
        // =====================================================

        private const int MaxKeywordTerms = 10;

        // "Welder, Pune ,iti certified" -> ["welder", "pune", "iti certified"]
        // Commas separate terms so multi-word phrases stay intact.
        private static List<string> ParseKeywordTerms(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<string>();

            return keyword
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .Where(t => t.Length > 0)
                .Distinct()
                .Take(MaxKeywordTerms)
                .ToList();
        }

        // Builds (p1 OR p2 OR ...) over a single shared parameter so EF Core
        // can translate it to SQL.
        private static Expression<Func<CandidateProfile, bool>> CombineWithOr(
            List<Expression<Func<CandidateProfile, bool>>> predicates)
        {
            var parameter = Expression.Parameter(typeof(CandidateProfile), "c");

            Expression? body = null;

            foreach (var predicate in predicates)
            {
                var replaced = new ParameterReplacer(
                    predicate.Parameters[0], parameter).Visit(predicate.Body)!;

                body = body == null ? replaced : Expression.OrElse(body, replaced);
            }

            return Expression.Lambda<Func<CandidateProfile, bool>>(
                body ?? Expression.Constant(true), parameter);
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterReplacer(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
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
                    .Where(x => !x.User.IsDeleted)   // hide self-deleted candidate accounts
                    .AsQueryable();

            // -----------------------------------------
            // Keyword
            // -----------------------------------------

            var keywordTerms = ParseKeywordTerms(request.Keyword);

            if (keywordTerms.Count > 0)
            {
                var matchAll = string.Equals(
                    request.KeywordMatch,
                    "all",
                    StringComparison.OrdinalIgnoreCase);

                // One predicate per term; each term may hit name, trade,
                // role, location or any skill.
                var termPredicates = keywordTerms
                    .Select(term => (Expression<Func<CandidateProfile, bool>>)(x =>
                        x.FullName.ToLower().Contains(term)
                        ||
                        (x.PrimaryTrade != null &&
                         x.PrimaryTrade.ToLower().Contains(term))
                        ||
                        (x.Role != null &&
                         x.Role.ToLower().Contains(term))
                        ||
                        (x.CurrentCity != null &&
                         x.CurrentCity.ToLower().Contains(term))
                        ||
                        (x.CurrentState != null &&
                         x.CurrentState.ToLower().Contains(term))
                        ||
                        x.Skills.Any(s =>
                            s.SkillName.ToLower().Contains(term))))
                    .ToList();

                if (matchAll)
                {
                    // Chained Where() calls = AND
                    foreach (var predicate in termPredicates)
                        query = query.Where(predicate);
                }
                else
                {
                    query = query.Where(CombineWithOr(termPredicates));
                }
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

            // Read once for the whole page; the cost is then looked up per
            // candidate from their main trade's band.
            var tradeUnlockCosts =
                await GetTradeUnlockCostMapAsync();

            foreach (var candidate in candidates)
            {
                var aiResult = request.JobId.HasValue
    ? await _jobMatchingService
        .CalculateMatchAsync(

            candidate.CandidateId,
            request.JobId.Value)
    : null;

                // ── Fetch job title once for label display ─────────────
                string? aiMatchedJobTitle = null;
                if (request.JobId.HasValue && aiResult != null)
                {
                    var job = await _context.JobPostings
                        .AsNoTracking()
                        .Where(j => j.JobId == request.JobId.Value)
                        .Select(j => new { j.JobTitle })
                        .FirstOrDefaultAsync();
                    aiMatchedJobTitle = job?.JobTitle;
                }
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

                        Role =
                            candidate.Role,

                        ExperienceYears =
                            candidate.TotalExperienceYears,

                        CurrentCity =
                            candidate.CurrentCity,

                        CurrentState =
                            candidate.CurrentState,

                        ExpectedSalary =
                            candidate.PreferredSalary,

                        AvailabilityStatus =
                            candidate.AvailabilityStatus,

                        AvailableIn =

                            candidate.AvailableIn,


                        KeywordMatchPercentage =
    aiResult?.MatchScore ?? 0,

                        AiMatchScore =
    aiResult?.MatchScore ?? 0,
                        MatchScore =
    aiResult?.MatchScore ?? 0,
                        MatchReason =
    aiResult?.MatchReason,

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
                            UnlockCostFor(
                                tradeUnlockCosts,
                                candidate.PrimaryTrade),

                        Skills =
                            skillNames,

                        AiMatchedJobTitle = aiMatchedJobTitle,

                        AiScoreBreakdown = aiResult == null ? null : new AiScoreBreakdownDto
                        {
                            OverallScore = aiResult.MatchScore,
                            AiSimilarityScore = aiResult.AiSimilarityScore,
                            SkillScore = aiResult.SkillScore,
                            TradeScore = aiResult.TradeScore,
                            ExperienceScore = aiResult.ExperienceScore,
                            LocationScore = aiResult.LocationScore,
                            ScoreLabel = aiResult.MatchScore switch
                            {
                                >= 80 => "Excellent",
                                >= 60 => "Good",
                                >= 40 => "Fair",
                                _ => "Low"
                            },
                            MatchReason = aiResult.MatchReason ?? string.Empty,
                            MatchedSkills = new(),
                            MissingSkills = new()
                        }
                    });
            }
            candidateCards =
    candidateCards
        .OrderByDescending(x => x.AiMatchScore)
        .ToList();
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
                        x.CandidateId == candidateId &&
                        !x.User.IsDeleted);

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

                AvailableIn =

                    candidate.AvailableIn,


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
            // A trade is only offered if it is still in use by a live
            // candidate profile (same visibility rule as SearchCandidatesAsync:
            // self-deleted accounts are excluded) or by an active job posting.
            // Because PrimaryTrade is overwritten when a candidate changes
            // their Job Title, the previous value drops out of this list
            // automatically once nobody else uses it, and stays listed while
            // any other user / active job still does. Nothing is deleted from
            // the trade master tables.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var candidateTrades =
                await _context.CandidateProfiles
                    .AsNoTracking()
                    .Where(x =>
                        !x.User.IsDeleted &&
                        x.PrimaryTrade != null &&
                        x.PrimaryTrade.Trim() != "")
                    .Select(x => x.PrimaryTrade!.Trim())
                    .Distinct()
                    .ToListAsync();

            var jobTrades =
                await _context.JobPostings
                    .AsNoTracking()
                    .Where(j =>
                        j.JobStatus == JobStatus.Active &&
                        j.IsActive &&
                        !j.IsDeleted &&
                        j.ApplicationDeadline >= today &&
                        j.TradeCategory != null &&
                        j.TradeCategory.Trim() != "")
                    .Select(j => j.TradeCategory!.Trim())
                    .Distinct()
                    .ToListAsync();

            var trades =
                candidateTrades
                    .Union(jobTrades, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            var cities =
                await _context.CandidateProfiles
                    .Where(x =>
                        !x.User.IsDeleted &&
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
                            x.CandidateId) &&
                        !x.User.IsDeleted)
                    .ToListAsync();

            var result =
                new List<CvSearchCandidateCardDto>();

            var tradeUnlockCosts =
                await GetTradeUnlockCostMapAsync();

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

                        Role =
                            candidate.Role,

                        ExperienceYears =
                            candidate.TotalExperienceYears,

                        CurrentCity =
                            candidate.CurrentCity,

                        CurrentState =
                            candidate.CurrentState,

                        ExpectedSalary =
                            candidate.PreferredSalary,

                        AvailabilityStatus =
                            candidate.AvailabilityStatus,
                        AvailableIn =
                            candidate.AvailableIn,

                        KeywordMatchPercentage = 0,
                        AiMatchScore = 0,
                        MatchReason = null,

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
                            UnlockCostFor(
                                tradeUnlockCosts,
                                candidate.PrimaryTrade),

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