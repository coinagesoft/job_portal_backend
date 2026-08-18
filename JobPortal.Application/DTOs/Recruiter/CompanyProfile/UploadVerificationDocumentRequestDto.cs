using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class UploadVerificationDocumentRequestDto
    {
        public Guid? DocumentTypeId { get; set; }

        // For requested admin documents
        public Guid? RequestId { get; set; }

        // Used for "Other" custom documents
        public string? CustomDocumentName { get; set; }

        // For predefined Additional document choices

        public IFormFile File { get; set; } = default!;
    }

 
}