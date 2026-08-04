namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class DocumentViewResponseDto
    {
        public Guid DocumentTypeId { get; set; }

        public string DocumentName { get; set; } = default!;

        public string Category { get; set; } = default!;

        public bool IsMandatory { get; set; }
    }
}