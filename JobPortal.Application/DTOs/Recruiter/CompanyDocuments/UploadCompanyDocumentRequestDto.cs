using Microsoft.AspNetCore.Http;

namespace JobPortal.Application.DTOs.Recruiter.CompanyDocuments
{
    public class UploadCompanyDocumentRequestDto
    {
        /// <summary>
        /// Selected document type from VerificationDocumentMaster.
        /// If "Other" is selected, backend will use Gemini to detect the type.
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Optional. Used only when recruiter selects "Other".
        /// Gemini may override this if it detects a known document type.
        /// </summary>
        public string? DocumentName { get; set; }

        /// <summary>
        /// Optional. Used only when recruiter selects "Other".
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Company document to upload.
        /// </summary>
        public IFormFile File { get; set; } = default!;
    }
}