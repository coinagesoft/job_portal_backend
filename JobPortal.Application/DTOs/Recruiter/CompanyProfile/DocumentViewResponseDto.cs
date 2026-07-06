using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class DocumentViewResponseDto
    {
        public DocumentType DocumentType { get; set; } = default!;

        public string? FileUrl { get; set; }
    }
}
