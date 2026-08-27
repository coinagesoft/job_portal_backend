using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{

    public class AdminRecruiterDetailDto
    {
        public string Id { get; set; } = default!;
        public string? Logo { get; set; }

        public string Company { get; set; } = default!;
        public string? AccountStatus { get; set; }

        public RecruiterInformationDto Recruiter { get; set; } = new();
        public RecruiterCompanyDto CompanyInformation { get; set; } = new();

        public RecruiterMembershipDto? Membership { get; set; }

        public List<RecruiterDocumentDto> Documents { get; set; }
            = new();

        public List<RecruiterBadgeDto> Badges { get; set; }
            = new();

        public RecruiterQuickInsightsDto QuickInsights { get; set; }
            = new();

        public RecruiterAccountHealthDto AccountHealth { get; set; }
            = new();

        public RecruiterPrimaryContactDto PrimaryContact { get; set; }
            = new();

        public List<RecruiterTransactionDto> Transactions { get; set; }
            = new();
    }

    public class RecruiterInformationDto
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
    }

    public class RecruiterCompanyDto
    {
        public string? LegalName { get; set; }
        public string? IndustryType { get; set; }
        public string? DisplayName { get; set; }
        public int TotalEmployees { get; set; }
        public short? FoundedYear { get; set; }
        public string? Address { get; set; }
        public string? BusinessType { get; set; }
        public string? CompanySize { get; set; }
        public string? CompanyType { get; set; }
        public string? Website { get; set; }

        // "RecruitmentAgency" | "Employer" — answered on registration
        // Step 2. Shown here so Admin knows *why* a given recruiter is
        // (or isn't) expected to have a Recruitment License, before
        // reviewing what they uploaded in the Documents section below.
        public string? NatureOfCompany { get; set; }

        // Whether this recruiter placed candidates internationally —
        // asked regardless of NatureOfCompany. When true, POE License
        // and RPSL License are expected in the Documents list below.
        public bool? PlacesCandidatesInternationally { get; set; }
    }

    public class RecruiterMembershipDto
    {
        public string? PlanName { get; set; }
        public int Credits { get; set; }
        public decimal Price { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class RecruiterDocumentDto
    {
        public Guid DocumentId { get; set; }

        public string? Title { get; set; }
        public string? SubTitle { get; set; }

        public string Status { get; set; } = default!;

        public string? FileName { get; set; }
        public string? FileUrl { get; set; }

        public string? DocumentNumber { get; set; }
        public string? IssuingAuthority { get; set; }

        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        public bool Expired { get; set; }

        public decimal? AiExtractionPercentage { get; set; }

        public string? DetectedDocumentType { get; set; }

        public DateTime UploadedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public string? Remarks { get; set; }
    }

    public class RecruiterBadgeDto
    {
        public Guid BadgeId { get; set; }
        public string? BadgeType { get; set; }
        public string BadgeStatus { get; set; } = default!;

        public string? RevocationReason { get; set; }

        public Guid? VerificationDocumentId { get; set; }

        public DateTime IssuedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string Label { get; set; } = default!;
        public bool Active { get; set; }
    }

    public class RecruiterQuickInsightsDto
    {
        public DateTime RegisteredOn { get; set; }

        public int TotalOpenJobs { get; set; }

        public int TotalJobPosts { get; set; }

        public int CurrentCredits { get; set; }
    }

    public class RecruiterAccountHealthDto
    {
        public int ProfileCompletion { get; set; }

        public List<string> Issues { get; set; }
            = new();
    }

    public class RecruiterPrimaryContactDto
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
    }

    public class RecruiterTransactionDto
    {
        public Guid TransactionId { get; set; }

        public DateTime Date { get; set; }

        public string Description { get; set; } = default!;

        public string Type { get; set; } = default!;

        public decimal Amount { get; set; }

        public string? Payment { get; set; }

        public string? TransactionNumber { get; set; }

        public string PaymentStatus { get; set; } = default!;

        public string? InvoiceNumber { get; set; }

        public DateOnly? InvoiceDate { get; set; }

        public string? InvoiceUrl { get; set; }
    }
}