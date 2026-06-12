using JobPortal.Application.DTOs.Recruiter.CandidateProfile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterCandidateProfileService
    {
        Task<RecruiterCandidateProfileResponseDto?>GetFullProfileAsync(
        Guid employerId,
        Guid candidateId);

        Task<CandidateUnlockStatusResponseDto>GetUnlockStatusAsync(
                Guid employerId,
                Guid candidateId);
    }
}
