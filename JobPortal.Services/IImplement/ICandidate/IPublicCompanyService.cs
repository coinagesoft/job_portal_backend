using JobPortal.Application.DTOs.Candidate;
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
        Task<PublicCompanyListResponseDto> GetCompaniesAsync();

        /// <summary>
        /// Public company profile with open jobs.
        /// </summary>
        Task<PublicCompanyDetailResponseDto> GetCompanyDetailAsync(Guid employerId);
    }
}
