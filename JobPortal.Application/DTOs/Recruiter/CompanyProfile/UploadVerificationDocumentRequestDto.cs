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

        public string? DocumentName { get; set; }

        public string? Category { get; set; }

        public IFormFile File { get; set; } = default!;
    }

    public enum DocumentType
    {
        POE,
        RPSL,
        BUSINESS_REGISTRATION,
        GST,
        PAN
    }
}