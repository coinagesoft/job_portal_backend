using System;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Admin.LegalPages
{
    /// <summary>Body for saving a draft (PUT) or publishing (POST .../publish).</summary>
    public class SaveLegalDocumentRequestDto
    {
        [Required]
        public string Content { get; set; } = default!;

        /// <summary>Optional. Defaults to today (UTC) when publishing if not supplied.</summary>
        public DateTime? EffectiveDate { get; set; }
    }
}