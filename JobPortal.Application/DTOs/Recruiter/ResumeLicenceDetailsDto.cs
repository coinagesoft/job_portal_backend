using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{

    public class ResumeLicenceDetailsDto
    {
        public List<ResumeRegistrationDocumentDto> Documents { get; set; }
            = new();
    }

    public class ResumeRegistrationDocumentDto
    {
        public Guid? DocumentTypeId { get; set; }

        public string? DocumentName { get; set; }

        public string? Category { get; set; }

        public string? FileUrl { get; set; }

        public string? Status { get; set; }
    }
}
