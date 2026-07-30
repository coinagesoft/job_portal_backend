using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationDocumentsRequestDto
    {
        public string SessionId { get; set; } = string.Empty;

        public List<RegistrationDocumentUploadDto> Documents { get; set; } = new();
    }
}
