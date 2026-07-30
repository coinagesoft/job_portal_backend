using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationDocumentUploadDto
    {
        public Guid? DocumentTypeId { get; set; }

        public string? DocumentName { get; set; }

        public string? Category { get; set; }

        public IFormFile File { get; set; } = default!;
    }
}
