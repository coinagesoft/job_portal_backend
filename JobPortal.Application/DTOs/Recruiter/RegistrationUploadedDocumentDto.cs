using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationUploadedDocumentDto
    {
        public Guid RegistrationDocumentId { get; set; }

        public Guid? DocumentTypeId { get; set; }

        public string DocumentName { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;

        public string Status { get; set; } = "Uploaded";
    }
}
