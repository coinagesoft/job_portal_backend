// ============================================================
//  JobPortal.Application/DTOs/Candidate/CandidateJobDtos.cs
//  DTOs for Candidate Job Listing, Search Filters & Job Detail
// ============================================================

using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.Candidate.Jobs;

// ─────────────────────────────────────────────────────────────
// 1.  SEARCH / FILTER REQUEST  (query-string params)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// All query-string filters sent from the jobs-list page.
/// Every field is optional — omit to skip that filter.
/// </summary>
public class CandidateJobSearchRequestDto
{
    // ── Keyword search ────────────────────────────────────────
    /// <summary>Free-text search across title, company, description, skills.</summary>
    public string? Keyword { get; set; }

    // ── Location ─────────────────────────────────────────────
    /// <summary>City name (e.g. "Mumbai", "Chennai").</summary>
    public string? Location { get; set; }

    /// <summary>State name (e.g. "Maharashtra").</summary>
    public string? State { get; set; }

    /// <summary>Onshore | Offshore</summary>
    public string? LocationType { get; set; }

    // ── Job type / trade ─────────────────────────────────────
    /// <summary>Trade category (e.g. "Welder", "Electrician").</summary>
    public string? TradeCategory { get; set; }

    /// <summary>Specific role (e.g. "Senior Welder").</summary>
    public string? Role { get; set; }

    // ── Experience ───────────────────────────────────────────
    /// <summary>Minimum years of experience required (e.g. 2 means "2+ years").</summary>
    public int? ExperienceYearsMin { get; set; }

    /// <summary>Maximum years of experience filter.</summary>
    public int? ExperienceYearsMax { get; set; }

    // ── Salary ───────────────────────────────────────────────
    /// <summary>Minimum salary filter (same currency as job).</summary>
    public int? SalaryMin { get; set; }

    /// <summary>Maximum salary filter.</summary>
    public int? SalaryMax { get; set; }

    /// <summary>INR | USD | AED | SAR</summary>
    public string? SalaryCurrency { get; set; }

    // ── Eligibility ───────────────────────────────────────────
    /// <summary>Gender filter: Male | Female | Any</summary>
    public string? Gender { get; set; }

    /// <summary>Education level: Any | Tenth | Twelfth | ITI | ITI_Diploma | Diploma | Graduate | Post_Graduate</summary>
    public string? EducationLevel { get; set; }

    /// <summary>Filter jobs that allow candidates with disabilities.</summary>
    public bool? DisabilityEligible { get; set; }

    /// <summary>Filter jobs that require a passport.</summary>
    public bool? PassportRequired { get; set; }

    // ── Employment type ───────────────────────────────────────
    /// <summary>Permanent | Contract | Temporary | Internship</summary>
    public string? EmploymentType { get; set; }

    /// <summary>Normal_Job | Hot_Vacancy | Classified</summary>
    public string? JobType { get; set; }

    // ── Freshness ─────────────────────────────────────────────
    /// <summary>
    /// Filter by how recently the job was posted.
    /// Values: "1" (last 24 h), "3" (3 days), "7" (7 days), "30" (30 days).
    /// </summary>
    public int? PostedWithinDays { get; set; }

    // ── Pagination ────────────────────────────────────────────
    /// <summary>Page number, 1-based. Default: 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page. Default: 12. Max: 50.</summary>
    public int PageSize { get; set; } = 12;

    // ── Sort ──────────────────────────────────────────────────
    /// <summary>
    /// Sort order: newest (default) | oldest | salary_high | salary_low
    /// </summary>
    public string Sort { get; set; } = "newest";
}

// ─────────────────────────────────────────────────────────────
// 2.  JOB CARD DTO  (used in list response)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Compact job card shown in the jobs-list page.
/// Matches every field the frontend JobCardList component reads.
/// </summary>
public class CandidateJobCardDto
{
    public Guid JobId { get; set; }

    // ── Company ──────────────────────────────────────────────
    /// <summary>Company name. Null/hidden when CompanyVisibility = Confidential_Client.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Company logo URL. Null when company is confidential.</summary>
    public string? CompanyLogoUrl { get; set; }

    public bool IsConfidentialCompany { get; set; }

    // ── Job basics ────────────────────────────────────────────
    public string JobTitle { get; set; } = default!;
    public string TradeCategory { get; set; } = default!;
    public string? Role { get; set; }
    public string JobType { get; set; } = default!;        // Normal_Job | Hot_Vacancy | Classified
    public string EmploymentType { get; set; } = default!; // Permanent | Contract | ...

    // ── Location ─────────────────────────────────────────────
    public string LocationType { get; set; } = default!;   // Onshore | Offshore
    public string? City { get; set; }
    public string? State { get; set; }
    public string? OffshoreRegion { get; set; }
    public bool IsInternational { get; set; }

    // ── Salary ───────────────────────────────────────────────
    /// <summary>Formatted salary string, e.g. "₹45,000 – ₹60,000 / month". Null when Confidential.</summary>
    public string? SalaryDisplay { get; set; }

    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string SalaryCurrency { get; set; } = "INR";

    // ── Experience & eligibility ──────────────────────────────
    public int ExperienceRequiredYears { get; set; }
    public string? EducationRequired { get; set; }
    public string GenderPreferred { get; set; } = "Any";
    public bool DisabilityEligible { get; set; }
    public bool PassportRequired { get; set; }

    // ── Openings & deadline ───────────────────────────────────
    public int Vacancies { get; set; }
    public DateOnly ApplicationDeadline { get; set; }
    public bool IsDeadlineSoon { get; set; }              // within 7 days

    // ── Meta ──────────────────────────────────────────────────
    public List<string> Tags { get; set; } = new();        // Hot_Vacancy, Urgent_Hiring etc.
    public List<string> KeySkills { get; set; } = new();

    /// <summary>Human-readable "2 hours ago" / "3 days ago" string.</summary>
    public string TimeAgo { get; set; } = default!;

    public DateTime? PublishedAt { get; set; }
    public int AppliedCount { get; set; }

    // ── Short description snippet ─────────────────────────────
    public string ShortDescription { get; set; } = default!;
    public string? EmploymentMode { get; set; }
    public string? Department { get; set; }

    public int ExperienceMinYears { get; set; }
    public int ExperienceMaxYears { get; set; }
    public string? ExperienceDisplay { get; set; }

    public byte? DutyHoursPerDay { get; set; }
    public bool PaidOvertime { get; set; }

    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsUrgentHiring { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 3.  JOB LIST RESPONSE
// ─────────────────────────────────────────────────────────────

public class CandidateJobListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public List<CandidateJobCardDto> Jobs { get; set; } = new();

    // ── Pagination metadata ───────────────────────────────────
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    // ── Active filters echo ───────────────────────────────────
    public CandidateJobSearchRequestDto AppliedFilters { get; set; } = default!;
}

// ─────────────────────────────────────────────────────────────
// 4.  JOB DETAIL RESPONSE  (full single-job view)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Full job details shown on the job-details page.
/// Includes everything from the card PLUS description, screening questions,
/// and employer info.
/// </summary>
public class CandidateJobDetailResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // ─────────────────────────────────────────────
    // Job
    // ─────────────────────────────────────────────

    public Guid JobId { get; set; }

    public string JobTitle { get; set; } = default!;
    public string TradeCategory { get; set; } = default!;
    public string? Role { get; set; }

    public string JobType { get; set; } = default!;

    public string EmploymentType { get; set; } = default!;

    public string EmploymentMode { get; set; } = default!;

    public string? Department { get; set; }

    public string JobDescription { get; set; } = default!;

    // ─────────────────────────────────────────────
    // Company
    // ─────────────────────────────────────────────

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public bool IsConfidentialCompany { get; set; }

    public string? CompanyWebsite { get; set; }

    public string? CompanyDescription { get; set; }

    public string? CompanyCity { get; set; }

    public string? CompanyState { get; set; }

    public string? CompanyAddress { get; set; }

    public string? CompanyPhone { get; set; }

    public string? CompanyEmail { get; set; }

    public string? CompanyIndustry { get; set; }

    public string? CompanySize { get; set; }

    public bool HasPoeLicence { get; set; }

    public bool HasRpslLicence { get; set; }

    // ─────────────────────────────────────────────
    // Location
    // ─────────────────────────────────────────────

    public string LocationType { get; set; } = default!;

    public string? WorkAddressLine { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? Pincode { get; set; }

    public string? OffshoreVesselName { get; set; }

    public string? OffshoreRegion { get; set; }

    public bool IsInternational { get; set; }

    // ─────────────────────────────────────────────
    // Salary
    // ─────────────────────────────────────────────

    public string? SalaryDisplay { get; set; }

    public int? SalaryMin { get; set; }

    public int? SalaryMax { get; set; }

    public string SalaryCurrency { get; set; } = "INR";

    // ─────────────────────────────────────────────
    // Experience
    // ─────────────────────────────────────────────

    public int ExperienceMinYears { get; set; }

    public int ExperienceMaxYears { get; set; }

    // ─────────────────────────────────────────────
    // Skills
    // ─────────────────────────────────────────────

    public List<string> KeySkills { get; set; } = new();

    public List<string> KeyResponsibilities { get; set; } = new();

    public List<string> Benefits { get; set; } = new();

    public string? LicenceDocsRequired { get; set; }

    public string? LanguageRequired { get; set; }

    // ─────────────────────────────────────────────
    // Eligibility
    // ─────────────────────────────────────────────

    public int Vacancies { get; set; }

    public string? EducationRequired { get; set; }

    public int? AgeMin { get; set; }

    public int? AgeMax { get; set; }

    public string GenderPreferred { get; set; } = "Any";

    public bool DisabilityEligible { get; set; }

    public bool PassportRequired { get; set; }

    public int? PassportValidityMonths { get; set; }

    // ─────────────────────────────────────────────
    // Employment Extras
    // ─────────────────────────────────────────────

    public byte? DutyHoursPerDay { get; set; }

    public bool PaidOvertime { get; set; }

    // ─────────────────────────────────────────────
    // Analytics
    // ─────────────────────────────────────────────

    public int AppliedCount { get; set; }

    public int ViewCount { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsUrgentHiring { get; set; }

    // ─────────────────────────────────────────────
    // Dates
    // ─────────────────────────────────────────────

    public DateOnly ApplicationDeadline { get; set; }

    public bool IsDeadlineSoon { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string TimeAgo { get; set; } = string.Empty;

    // ─────────────────────────────────────────────
    // Tags
    // ─────────────────────────────────────────────

    public List<string> Tags { get; set; } = new();

    // ─────────────────────────────────────────────
    // Screening
    // ─────────────────────────────────────────────

    public List<string> ScreeningQuestions { get; set; } = new();

    // ─────────────────────────────────────────────
    // Similar Jobs
    // ─────────────────────────────────────────────

    public List<CandidateJobCardDto> SimilarJobs { get; set; } = new();
}
// ─────────────────────────────────────────────────────────────
// 5.  SCREENING QUESTION (readonly — displayed to candidate)
// ─────────────────────────────────────────────────────────────

public class CandidateScreeningQuestionDto
{
    public string QuestionText { get; set; } = default!;

    /// <summary>Yes_No | Text</summary>
    public string AnswerType { get; set; } = "Yes_No";

    public bool IsMandatory { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 6.  SAVE / UNSAVE JOB  (bookmark)
// ─────────────────────────────────────────────────────────────

public class SaveJobResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public bool IsSaved { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 7.  FILTER OPTIONS  (populate sidebar dropdowns)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Dynamic filter options returned from the DB (distinct values from live jobs).
/// Used to populate sidebar checkboxes / dropdowns on the jobs-list page.
/// </summary>
public class JobFilterOptionsResponseDto
{
    public bool Success { get; set; }

    public List<string> TradeCategories { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public List<string> States { get; set; } = new();
    public List<string> LocationTypes { get; set; } = new();
    public List<string> EmploymentTypes { get; set; } = new();
    public List<string> EducationLevels { get; set; } = new();
    public List<string> Currencies { get; set; } = new();
    public List<string> GenderOptions { get; set; } = new();
    public int MaxSalary { get; set; }
    public int MaxExperienceYears { get; set; }
    public int TotalActiveJobs { get; set; }
    public List<string> Countries { get; set; } = new();

    public List<string> EmploymentModes { get; set; } = new();

    public List<string> Departments { get; set; } = new();

    public List<string> Skills { get; set; } = new();

    public List<string> Benefits { get; set; } = new();

    public int MinSalary { get; set; }

    public int MinExperienceYears { get; set; }

    public bool HasFeaturedJobs { get; set; }

    public bool HasUrgentHiringJobs { get; set; }

    public bool HasInternationalJobs { get; set; }

    public bool HasPassportRequiredJobs { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 8.  SAVED JOBS LIST  (candidate-profile/saved-jobs page)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// A single saved-job card as shown on the Saved Jobs page.
/// Mirrors the card UI: company logo, title, type, experience,
/// salary, tags, and application status note.
/// </summary>
public class SavedJobCardDto
{
    public Guid SavedJobId { get; set; }
    public Guid JobId { get; set; }
    public DateTime SavedAt { get; set; }

    // ── Company ───────────────────────────────────────────────
    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }

    // ── Job basics ────────────────────────────────────────────
    public string JobTitle { get; set; } = default!;
    public string TradeCategory { get; set; } = default!;
    public string? Role { get; set; }
    public string EmploymentType { get; set; } = default!;
    public string JobType { get; set; } = default!;

    // ── Location ─────────────────────────────────────────────
    public string? City { get; set; }
    public string? State { get; set; }
    public string LocationDisplay { get; set; } = default!;

    // ── Experience ───────────────────────────────────────────
    public string ExperienceDisplay { get; set; } = default!;

    // ── Salary ───────────────────────────────────────────────
    public string? SalaryDisplay { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string SalaryCurrency { get; set; } = "INR";

    // ── Tags / skills ─────────────────────────────────────────
    public List<string> Tags { get; set; } = new();
    public List<string> KeySkills { get; set; } = new();

    // ── Deadline & freshness ──────────────────────────────────
    public DateOnly ApplicationDeadline { get; set; }
    public bool IsDeadlineSoon { get; set; }
    public bool IsExpired { get; set; }
    public string TimeAgo { get; set; } = default!;

    // ── Application state for this candidate ─────────────────
    /// <summary>
    /// Null = not applied yet.
    /// Non-null = Applied | Viewed | Shortlisted | Interview | Rejected | Hired | Withdrawn
    /// </summary>
    public string? ApplicationStatus { get; set; }
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// Human-readable status note on the card, e.g.
    /// "Employer shortlisted your profile for electrical maintenance …"
    /// </summary>
    public string? StatusNote { get; set; }
    public string? EmploymentMode { get; set; }

    public string? Department { get; set; }

    public int AppliedCount { get; set; }

    public int ViewCount { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsUrgentHiring { get; set; }
}

public class SavedJobListResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<SavedJobCardDto> SavedJobs { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public int AppliedCount { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 9.  APPLY NOW — request & response
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Posted when candidate clicks "Apply Now" and submits the apply modal.
/// Carries candidate details (editable in modal) + screening question answers.
/// </summary>
public class ApplyJobRequestDto
{
    /// <summary>Candidate full name (pre-filled from profile, editable).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Phone shown in the modal.</summary>
    public string? Phone { get; set; }

    /// <summary>Email shown in the modal.</summary>
    public string? Email { get; set; }

    /// <summary>
    /// One entry per employer screening question.
    /// Must include all mandatory questions.
    /// </summary>
    /// <summary>Set to true to confirm passport requirement is met.</summary>
    public bool? PassportGatePassed { get; set; }

    public List<ScreeningAnswerDto> ScreeningAnswers { get; set; } = new();
}

public class ScreeningAnswerDto
{
    /// <summary>Question text — echoed back so employer sees context.</summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>"Yes" | "No" for radio questions; free text for text questions.</summary>
    public string Answer { get; set; } = string.Empty;
}

public class ApplyJobResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public Guid? ApplicationId { get; set; }
    public Guid? JobId { get; set; }
    public string? ApplicationStatus { get; set; }
    public DateTime? AppliedAt { get; set; }

    // Echoed back for confirmation screen
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 10.  MY APPLICATIONS LIST
// ─────────────────────────────────────────────────────────────

public class MyApplicationCardDto
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }

    public string? CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public bool IsConfidentialCompany { get; set; }

    public string JobTitle { get; set; } = default!;
    public string EmploymentType { get; set; } = default!;
    public string ExperienceDisplay { get; set; } = default!;
    public string LocationDisplay { get; set; } = default!;
    public string? SalaryDisplay { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? City { get; set; }
    public string? State { get; set; }

    public string? TradeCategory { get; set; }
    /// <summary>Applied | Viewed | Shortlisted | Interview | Rejected | Hired | Withdrawn</summary>
    public string ApplicationStatus { get; set; } = default!;
    public DateTime AppliedAt { get; set; }
    public string AppliedTimeAgo { get; set; } = default!;
    public DateTime StatusUpdatedAt { get; set; }
    public bool WithdrawalAllowed { get; set; }
}

public class MyApplicationsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<MyApplicationCardDto> Applications { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int RejectedCount { get; set; }
    public int HiredCount { get; set; }
}

// ─────────────────────────────────────────────────────────────
// 11.  WITHDRAW APPLICATION
// ─────────────────────────────────────────────────────────────

public class WithdrawApplicationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string? ApplicationStatus { get; set; }
}