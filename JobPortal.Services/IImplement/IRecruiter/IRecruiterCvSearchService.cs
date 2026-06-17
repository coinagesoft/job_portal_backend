using JobPortal.Application.DTOs.Recruiter.CVSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterCvSearchService
    {
        /// <summary>
        /// CV Search dashboard counts
        /// </summary>
        Task<CvSearchDashboardDto> GetDashboardAsync(
            Guid employerId);

        /// <summary>
        /// Search candidates with filters
        /// </summary>
        Task<CvSearchResponseDto> SearchCandidatesAsync(
            Guid employerId,
            CvSearchRequestDto request);

        /// <summary>
        /// Candidate preview card details
        /// </summary>
        Task<CandidatePreviewDto?> GetCandidatePreviewAsync(
            Guid employerId,
            Guid candidateId);

        /// <summary>
        /// Trade, Location and Availability dropdown values
        /// </summary>
        Task<CvSearchFilterOptionsDto> GetFilterOptionsAsync();

        /// <summary>
        /// Already unlocked candidates for recruiter
        /// </summary>
        Task<List<CvSearchCandidateCardDto>> GetUnlockedCandidatesAsync(
            Guid employerId);
    }
}
