using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Application.DTOs.Candidate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminCandidateService
    {
        Task<List<AdminCandidateListItemDto>> GetCandidatesAsync();
        Task<AdminCandidateDetailDto?> GetCandidateDetailAsync(Guid candidateId);

        Task<bool> UpdateAccountStatusAsync(Guid candidateId, UpdateAccountStatusRequestDto request, AdminAuditContext audit);
    }
}
