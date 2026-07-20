using JobPortal.Application.DTOs.Recruiter.CandidateProfile;
using JobPortal.Application.DTOs.Recruiter.CVSearch;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.AI;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterCandidateProfileService
        : IRecruiterCandidateProfileService
    {
        private readonly AppDbContext _context;
        private readonly IRankedCandidateService _rankedCandidateService;

        public RecruiterCandidateProfileService(
            AppDbContext context,
            IRankedCandidateService rankedCandidateService)
        {
            _context = context;
            _rankedCandidateService = rankedCandidateService;
        }

        public async Task<RecruiterCandidateProfileResponseDto?>
            GetFullProfileAsync(
                Guid employerId,
                Guid candidateId,
                Guid? jobId = null)
        {
            var candidate = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.Skills)
                .Include(x => x.Educations)
                .Include(x => x.WorkHistories)
                .Include(x => x.Cvs)
                .FirstOrDefaultAsync(
                    x => x.CandidateId == candidateId);

            if (candidate == null)
            {
                return null;
            }

            var access =
                await _context.EmployerCandidateAccesses
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId &&
                    x.CandidateId == candidateId &&
                    x.IsActive &&
                    x.ExpiresAt >= DateTime.UtcNow);

            var isUnlocked = access != null;

            // ── AI Score (optional – only computed when caller passes a jobId) ──
            AiScoreBreakdownDto? aiBreakdown = null;
            string? aiMatchedJobTitle = null;

            if (jobId.HasValue && jobId.Value != Guid.Empty)
            {
                var scoreResult = await _rankedCandidateService
                    .GetCandidateProfileScoreAsync(candidateId, jobId.Value);

                if (scoreResult != null)
                {
                    aiMatchedJobTitle = scoreResult.JobTitle;
                    aiBreakdown = new AiScoreBreakdownDto
                    {
                        OverallScore = scoreResult.ScoreBreakdown.OverallScore,
                        AiSimilarityScore = scoreResult.ScoreBreakdown.AiSimilarityScore,
                        SkillScore = scoreResult.ScoreBreakdown.SkillScore,
                        TradeScore = scoreResult.ScoreBreakdown.TradeScore,
                        ExperienceScore = scoreResult.ScoreBreakdown.ExperienceScore,
                        LocationScore = scoreResult.ScoreBreakdown.LocationScore,
                        ScoreLabel = scoreResult.ScoreBreakdown.ScoreLabel,
                        MatchReason = scoreResult.ScoreBreakdown.MatchReason,
                        MatchedSkills = scoreResult.MatchedSkills,
                        MissingSkills = scoreResult.MissingSkills
                    };
                }
            }

            var latestCv = candidate.Cvs
                .OrderByDescending(x => x.GeneratedAt)
                .FirstOrDefault();

            // ── Related candidates (same trade, different candidate) ──
            var relatedCandidates = new List<RelatedCandidateCardDto>();

            if (!string.IsNullOrWhiteSpace(candidate.PrimaryTrade))
            {
                var trade = candidate.PrimaryTrade.Trim();

                relatedCandidates = await _context.CandidateProfiles
                    .AsNoTracking()
                    .Where(x =>
                        x.CandidateId != candidateId &&
                        x.PrimaryTrade != null &&
                        x.PrimaryTrade.ToLower() == trade.ToLower())
                    .OrderByDescending(x => x.LastAppliedAt ?? x.UpdatedAt)
                    .Take(5)
                    .Select(x => new RelatedCandidateCardDto
                    {
                        CandidateId = x.CandidateId,
                        FullName = x.FullName,
                        ProfilePhotoUrl = x.ProfilePhotoUrl,
                        PrimaryTrade = x.PrimaryTrade,
                        TotalExperienceYears = x.TotalExperienceYears,
                        CurrentCity = x.CurrentCity,
                        CurrentState = x.CurrentState,
                        AvailabilityStatus = x.AvailabilityStatus
                    })
                    .ToListAsync();
            }

            var response =
                new RecruiterCandidateProfileResponseDto
                {
                    Overview =
                        new CandidateOverviewResponseDto
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

                            TotalExperienceYears =
                                candidate.TotalExperienceYears,

                            CurrentCity =
                                candidate.CurrentCity,

                            CurrentState =
                                candidate.CurrentState,

                            AvailabilityStatus =
                                candidate.AvailabilityStatus,

                            NoticePeriod =
                                candidate.NoticePeriod,

                            AiMatchScore =
                                candidate.AiMatchScore,

                            IsUnlocked =
                                isUnlocked,

                            AiMatchedJobTitle = aiMatchedJobTitle,
                            AiScoreBreakdown = aiBreakdown,
                        },

                    Summary =
                        new CandidateSummaryResponseDto
                        {
                            About =
                                candidate.About,

                            ProfessionalSummary =
                                candidate.ProfessionalSummary,

                            Nationality =
                                candidate.Nationality,

                            PreferredSalary =
                                isUnlocked
                                    ? candidate.PreferredSalary
                                    : null,

                            DisabilityStatus =
                                candidate.DisabilityStatus,

                            DisabilityNote =
                                candidate.DisabilityNote,

                            ItiCertified =
                                candidate.ItiCertified,

                            ItiTrade =
                                candidate.ItiTrade,

                            ItiCollege =
                                candidate.ItiCollege,

                            ItiMarks =
                                candidate.ItiMarks
                        },

                    Skills =
                        candidate.Skills
                        .Where(x =>
                            x.SkillType == "Skill")
                        .Select(x =>
                            new CandidateSkillDto
                            {
                                SkillName =
                                    x.SkillName,

                                YearsOfExperience =
                                    x.YearsOfExperience,

                                SkillRole =
                                    x.SkillRole
                            })
                        .ToList(),

                    Languages =
                        candidate.Skills
                        .Where(x =>
                            x.SkillType == "Language")
                        .Select(x =>
                            new CandidateLanguageDto
                            {
                                Language =
                                    x.SkillName,

                                CanRead =
                                    x.CanRead,

                                CanWrite =
                                    x.CanWrite,

                                CanSpeak =
                                    x.CanSpeak
                            })
                        .ToList(),

                    Educations =
                        candidate.Educations
                        .Select(x =>
                            new CandidateEducationDto
                            {
                                EducationId =
                                    x.EducationId,

                                EducationLevel =
                                    x.EducationLevel,

                                InstituteName =
                                    x.InstituteName,

                                PassoutYear =
                                    x.PassoutYear,

                                IsAiVerified =
                                    x.IsAiVerified,

                                CertificateUrl =
                                    isUnlocked
                                        ? x.CertificateUrl
                                        : null
                            })
                        .ToList(),

                    WorkHistories =
                        candidate.WorkHistories
                        .OrderByDescending(x => x.StartDate)
                        .Select(x =>
                            new CandidateWorkHistoryDto
                            {
                                WorkId =
                                    x.WorkId,

                                CompanyName =
                                    x.CompanyName,

                                JobTitle =
                                    x.JobTitle,

                                StartDate =
                                    x.StartDate,

                                EndDate =
                                    x.EndDate,

                                IsCurrent =
                                    x.IsCurrent,

                                WorkLocation =
                                    x.WorkLocation,

                                IsOffshore =
                                    x.IsOffshore,

                                JobDescription =
                                    x.JobDescription
                            })
                        .ToList(),

                    Cv =
                        latestCv == null
                            ? null
                            : new CandidateCvResponseDto
                            {
                                CvId =
                                    latestCv.CvId,

                                ParsedTrade =
                                    latestCv.ParsedTrade,

                                ParsedExperienceYrs =
                                    latestCv.ParsedExperienceYrs,

                                AiConfidenceScore =
                                    latestCv.AiConfidenceScore,

                                GeneratedAt =
                                    latestCv.GeneratedAt,

                                CvAvailable =
                                    !string.IsNullOrWhiteSpace(
                                        latestCv.CvFileUrl),

                                CanDownloadCv =
                                    isUnlocked
                            },

                    UnlockStatus =
                        new CandidateUnlockStatusResponseDto
                        {
                            IsUnlocked =
                                isUnlocked,

                            UnlockDate =
                                access?.GrantedAt,

                            ExpiryDate =
                                access == null
                                    ? null
                                    : DateOnly.FromDateTime(
                                        access.ExpiresAt),

                            CvDownloadAllowed =
                                isUnlocked
                        },

                    RelatedCandidates =
                        relatedCandidates
                };

            return response;
        }


        public async Task<CandidateUnlockStatusResponseDto> GetUnlockStatusAsync(
                Guid employerId,
                Guid candidateId)
        {
            var access =
                await _context.EmployerCandidateAccesses
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId &&
                    x.CandidateId == candidateId &&
                    x.IsActive &&
                    x.ExpiresAt >= DateTime.UtcNow);

            if (access == null)
            {
                return new CandidateUnlockStatusResponseDto
                {
                    IsUnlocked = false,
                    CvDownloadAllowed = false
                };
            }

            return new CandidateUnlockStatusResponseDto
            {
                IsUnlocked = true,
                UnlockDate = access.GrantedAt,
                ExpiryDate =
                    DateOnly.FromDateTime(
                        access.ExpiresAt),
                CvDownloadAllowed = true
            };
        }


    }
}