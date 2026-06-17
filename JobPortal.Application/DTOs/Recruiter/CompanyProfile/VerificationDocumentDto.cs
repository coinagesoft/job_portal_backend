using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class VerificationDocumentDto
    {
        public string DocumentType { get; set; } = default!;

        public string? FileUrl { get; set; }

        public string Status { get; set; } = default!;

        public DateTime? UploadedAt { get; set; }
    }
}
