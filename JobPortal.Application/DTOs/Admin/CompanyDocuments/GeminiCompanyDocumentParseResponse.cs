using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{

    public class GeminiCompanyDocumentParseResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public string? DocumentType { get; set; }

        public decimal? AiConfidenceScore { get; set; }

        public JsonElement? ParsedData { get; set; }

        public string? RawResponse { get; set; }

        // These will be populated later from ParsedData
        public string? DocumentNumber { get; set; }

        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }
    }
}
