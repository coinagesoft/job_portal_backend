using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class AdminCandidateDetailDto
    {
        // Profile header
        public string Id { get; set; } = default!;
        public string? Img { get; set; }
        public string AccountStatus { get; set; } = default!;
        public byte CompletenessPct { get; set; }
        public string Name { get; set; } = default!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string RegisteredOn { get; set; } = default!;

        // Quick stats
        public string? TradeCategory { get; set; }
        public string? Location { get; set; }
        public string Experience { get; set; } = default!;
        public string PaymentStatus { get; set; } = default!;
        public bool AvailableForWork { get; set; }

        // Billing & Documents
        public List<AdminCandidateBillingItemDto> Billing { get; set; } = new();
        public List<AdminCandidateDocumentItemDto> Documents { get; set; } = new();
    }

    public class AdminCandidateBillingItemDto
    {
        public string TransactionId { get; set; } = default!;
        public string Date { get; set; } = default!;
        public string Amount { get; set; } = default!;
        public string Status { get; set; } = default!;
    }

    public class AdminCandidateDocumentItemDto
    {
        public string DocId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Subtitle { get; set; } = "Document"; // no category source on CandidateDocument — see note below
        public string Url { get; set; } = default!;
        public string UploadedOn { get; set; } = default!;
        public string VerificationStatus { get; set; } = default!;
    }


}
