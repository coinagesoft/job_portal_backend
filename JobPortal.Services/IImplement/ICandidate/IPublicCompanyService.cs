using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Candidate.Jobs;
using JobPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.ICandidate
{
    public interface IPublicCompanyService
    {
        /// <summary>
        /// Public list of companies shown before login.
        /// </summary>
        /// 
        Task<List<CandidateJobListItemDto>> GetAllJobsAsync(Guid? candidateId = null);
        Task<CandidateJobDetailsDto?> GetJobDetailsAsync(Guid jobId, Guid? candidateId = null);

        Task<CandidateJobListResponseDto> GetJobsAsync(CandidateJobSearchRequestDto request, Guid? candidateId = null);

        Task<PublicCompanyListResponseDto> GetCompaniesAsync();

        /// <summary>
        /// Public company profile with open jobs.
        /// </summary>
        Task<PublicCompanyDetailResponseDto> GetCompanyDetailAsync(Guid employerId);
    }
}