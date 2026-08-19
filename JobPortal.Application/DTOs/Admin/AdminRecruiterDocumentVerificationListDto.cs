using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



    namespace JobPortal.Application.DTOs.Admin
    {
        public class AdminRecruiterDocumentVerificationListDto
        {
            // ==================================================
            // IDS
            // ==================================================

            public Guid? DocumentId { get; set; }

            public Guid? DocumentTypeId { get; set; }

            public Guid? RequestId { get; set; }


            // ==================================================
            // DOCUMENT
            // ==================================================

            public string DocumentName { get; set; } = string.Empty;


            // ==================================================
            // DOCUMENT TYPE
            // ==================================================
            //
            // Example:
            // GST
            // PAN
            // Company Registration
            //
            public string? DocumentType { get; set; }


            // ==================================================
            // DOCUMENT CATEGORY
            // ==================================================
            //
            // Mandatory
            // Optional
            // Additional
            // RequestedAdditional
            //
            public string DocumentCategory { get; set; } = string.Empty;


            // ==================================================
            // DOCUMENT TYPE CATEGORY
            // ==================================================
            //
            // Example:
            // Tax
            // License
            // Registration
            // Identity
            // Other
            //
            public string? DocumentTypeCategory { get; set; }


            // ==================================================
            // VERIFICATION STATUS
            // ==================================================

            public string DocumentVerificationStatus { get; set; } = string.Empty;
        }
    }
