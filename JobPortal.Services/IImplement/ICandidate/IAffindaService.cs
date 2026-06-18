using JobPortal.Application.DTOs.AI;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.ICandidate
{
    public interface IAffindaService
    {
        Task<AffindaParseResult> ParseResumeAsync(
              IFormFile file);
    }
}
