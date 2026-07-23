namespace JobPortal.Application.DTOs.Admin.CompanyDocuments
{
    public class UpdateDocumentTypeRequestDto
    {
        /// <summary>
        /// Flag only — reserved for future registration-completion logic.
        /// Not read or enforced anywhere else in this module.
        /// </summary>
        public bool? IsMandatory { get; set; }
        public bool? IsActive { get; set; }
        public bool? RequiresVerification { get; set; }
    }
}
