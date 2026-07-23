using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IDocumentTypeService
    {
        Task<List<RecruiterDocumentTypeDto>> GetActiveDocumentTypesAsync(Guid employerId);
    }
}
