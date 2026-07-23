namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{
    public class VerifyCompanyDocumentRequestDto
    {
        /// <summary>true = approve, false = reject.</summary>
        public bool Approve { get; set; }

        /// <summary>Required when rejecting; optional when approving.</summary>
        public string? Remarks { get; set; }
    }
}
