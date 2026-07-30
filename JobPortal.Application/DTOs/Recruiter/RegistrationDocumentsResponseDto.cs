using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationDocumentsResponseDto
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public List<RegistrationUploadedDocumentDto> Documents { get; set; } = new();

        public StepStatusDto? StepStatus { get; set; }
    }
}
