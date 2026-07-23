using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{

    public interface IGeminiCompanyDocumentParserService
    {
        Task<GeminiCompanyDocumentParseResponse> ParseDocumentAsync(IFormFile document);
    }
}
