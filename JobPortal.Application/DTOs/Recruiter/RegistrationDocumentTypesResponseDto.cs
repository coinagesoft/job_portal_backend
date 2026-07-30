using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationDocumentTypesResponseDto
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public List<RegistrationDocumentTypeDto> MandatoryDocuments { get; set; } = new();

        public List<RegistrationDocumentTypeDto> OptionalDocuments { get; set; } = new();
    }
}
