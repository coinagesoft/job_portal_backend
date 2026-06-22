
﻿using JobPortal.Application.DTOs.AI;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

﻿// ============================================================
//  JobPortal.Services/IImplement/ICandidate/IAffindaService.cs
//  REPLACES the old file that returned Task<string>
// ============================================================


using JobPortal.Application.DTOs.AI;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.ICandidate;

public interface IAffindaService
{

    public interface IAffindaService
    {
        Task<AffindaParseResult> ParseResumeAsync(
              IFormFile file);
    }

    /// <summary>
    /// Upload a resume file to Affinda, poll until ready:true,
    /// then return fully-mapped parsed fields.
    /// </summary>
    Task<AffindaParseResult> ParseResumeAsync(IFormFile file);

}
