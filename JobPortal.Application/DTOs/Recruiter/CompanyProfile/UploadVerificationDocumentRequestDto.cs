using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class UploadVerificationDocumentRequestDto
    {
        public string DocumentType { get; set; } = default!;

        public IFormFile File { get; set; } = default!;
    }
}
