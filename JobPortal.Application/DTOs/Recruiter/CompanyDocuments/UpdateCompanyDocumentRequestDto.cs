using Microsoft.AspNetCore.Http;
using System;

namespace JobPortal.Application.DTOs.Recruiter.CompanyDocuments
{
    public class UpdateCompanyDocumentRequestDto
    {
        /// <summary>
        /// Only editable when the document is free-text (no DocumentTypeId).
        /// Ignored for master-linked documents.
        /// </summary>
        public string? DocumentName { get; set; }
        public string? Category { get; set; }

        public string? DocumentNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        /// <summary>Optional — replace the uploaded file.</summary>
        public IFormFile? File { get; set; }
    }
}
