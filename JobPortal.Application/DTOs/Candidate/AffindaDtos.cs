// ============================================================
//  JobPortal.Application/DTOs/AI/AffindaDtos.cs
//
//  These DTOs map 1:1 to the Affinda API v3 resume response.
//  Field names match exactly what Affinda returns in data{}.
// ============================================================

using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.AI;

// ── Top-level wrapper (Affinda returns an array) ────────────
public class AffindaDocumentListResponse
{
    [JsonPropertyName("data")] public AffindaResumeData? Data { get; set; }
    [JsonPropertyName("meta")] public AffindaMeta? Meta { get; set; }
    [JsonPropertyName("error")] public AffindaError? Error { get; set; }
}

// ── Single document (what you get from GET /v3/documents/{id}) ─
public class AffindaSingleDocumentResponse
{
    [JsonPropertyName("data")] public AffindaResumeData? Data { get; set; }
    [JsonPropertyName("meta")] public AffindaMeta? Meta { get; set; }
    [JsonPropertyName("error")] public AffindaError? Error { get; set; }
}

// ── Meta block ──────────────────────────────────────────────
public class AffindaMeta
{
    [JsonPropertyName("identifier")] public string? Identifier { get; set; }
    [JsonPropertyName("ready")] public bool Ready { get; set; }
    [JsonPropertyName("failed")] public bool Failed { get; set; }
    [JsonPropertyName("readyDt")] public DateTime? ReadyDt { get; set; }
    [JsonPropertyName("fileName")] public string? FileName { get; set; }
    [JsonPropertyName("ocrConfidence")] public decimal? OcrConfidence { get; set; }
}

// ── Error block ─────────────────────────────────────────────
public class AffindaError
{
    [JsonPropertyName("errorCode")] public string? ErrorCode { get; set; }
    [JsonPropertyName("errorDetail")] public string? ErrorDetail { get; set; }
}

// ══════════════════════════════════════════════════════════════
// RESUME DATA — data{} block
// ══════════════════════════════════════════════════════════════
public class AffindaResumeData
{
    [JsonPropertyName("candidateName")] public AffindaCandidateName? CandidateName { get; set; }
    [JsonPropertyName("email")] public List<string>? Email { get; set; }
    [JsonPropertyName("phoneNumber")] public List<AffindaPhoneNumber>? PhoneNumber { get; set; }
    [JsonPropertyName("location")] public AffindaLocation? Location { get; set; }
    [JsonPropertyName("dateOfBirth")] public string? DateOfBirth { get; set; }
    [JsonPropertyName("nationality")] public string? Nationality { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("totalYearsExperience")] public decimal? TotalYearsExperience { get; set; }
    [JsonPropertyName("skill")] public List<AffindaSkill>? Skill { get; set; }
    [JsonPropertyName("education")] public List<AffindaEducation>? Education { get; set; }
    [JsonPropertyName("workExperience")] public List<AffindaWorkExp>? WorkExperience { get; set; }
    [JsonPropertyName("language")] public List<AffindaLanguage>? Language { get; set; }
    [JsonPropertyName("rawText")] public string? RawText { get; set; }
}

// ── Name ────────────────────────────────────────────────────
public class AffindaCandidateName
{
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("familyName")] public string? FamilyName { get; set; }
    [JsonPropertyName("middleName")] public string? MiddleName { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
}

// ── Phone ────────────────────────────────────────────────────
public class AffindaPhoneNumber
{
    [JsonPropertyName("rawText")] public string? RawText { get; set; }
    [JsonPropertyName("formattedNumber")] public string? FormattedNumber { get; set; }
    [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
    [JsonPropertyName("internationalCountryCode")] public int? InternationalCountryCode { get; set; }
}

// ── Location ─────────────────────────────────────────────────
public class AffindaLocation
{
    [JsonPropertyName("formatted")] public string? Formatted { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("stateCode")] public string? StateCode { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
    [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
}

// ── Skill ─────────────────────────────────────────────────────
public class AffindaSkill
{
    [JsonPropertyName("Id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }  // "Specialized Skill" | "Common Skill"
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("subCategory")] public string? SubCategory { get; set; }
    [JsonPropertyName("isSoftware")] public bool IsSoftware { get; set; }
    [JsonPropertyName("isLanguage")] public bool IsLanguage { get; set; }
}

// ── Education ────────────────────────────────────────────────
public class AffindaEducation
{
    [JsonPropertyName("educationAccreditation")] public string? EducationAccreditation { get; set; }
    [JsonPropertyName("educationOrganization")] public string? EducationOrganization { get; set; }
    [JsonPropertyName("educationLevel")] public AffindaLabelValue? EducationLevel { get; set; }
    [JsonPropertyName("educationMajor")] public List<string>? EducationMajor { get; set; }
    [JsonPropertyName("educationDates")] public AffindaEduDates? EducationDates { get; set; }
    [JsonPropertyName("educationGrade")] public AffindaEduGrade? EducationGrade { get; set; }
    [JsonPropertyName("educationLocation")] public AffindaLocation? EducationLocation { get; set; }
}

public class AffindaEduDates
{
    [JsonPropertyName("start")] public AffindaDatePoint? Start { get; set; }
    [JsonPropertyName("end")] public AffindaDatePoint? End { get; set; }
    [JsonPropertyName("durationInMonths")] public int? DurationInMonths { get; set; }
}

public class AffindaDatePoint
{
    [JsonPropertyName("year")] public int? Year { get; set; }
    [JsonPropertyName("month")] public int? Month { get; set; }
    [JsonPropertyName("day")] public int? Day { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("isCurrent")] public bool IsCurrent { get; set; }
}

public class AffindaEduGrade
{
    [JsonPropertyName("educationGradeScore")] public decimal? EducationGradeScore { get; set; }
    [JsonPropertyName("gradeScore")] public decimal? GradeScore { get; set; }
    [JsonPropertyName("gradeUnit")] public AffindaLabelValue? GradeUnit { get; set; }
}

// ── Work Experience ──────────────────────────────────────────
public class AffindaWorkExp
{
    [JsonPropertyName("workExperienceJobTitle")] public string? JobTitle { get; set; }
    [JsonPropertyName("workExperienceOrganization")] public string? Organization { get; set; }
    [JsonPropertyName("workExperienceLocation")] public AffindaLocation? Location { get; set; }
    [JsonPropertyName("workExperienceDates")] public AffindaWorkDates? Dates { get; set; }
    [JsonPropertyName("workExperienceDescription")] public string? Description { get; set; }
    [JsonPropertyName("workExperienceType")] public AffindaLabelValue? Type { get; set; }
}

public class AffindaWorkDates
{
    [JsonPropertyName("start")] public AffindaDatePoint? Start { get; set; }
    [JsonPropertyName("end")] public AffindaDatePoint? End { get; set; }
    [JsonPropertyName("durationInMonths")] public int? DurationInMonths { get; set; }
}

// ── Language ─────────────────────────────────────────────────
public class AffindaLanguage
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("languageCode")] public string? LanguageCode { get; set; }
}

// ── Shared ───────────────────────────────────────────────────
public class AffindaLabelValue
{
    [JsonPropertyName("Id")] public string? Id { get; set; }
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("value")] public string? Value { get; set; }
}

// ── Result returned by AffindaService to CandidateDocumentService ──
public class AffindaParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AffindaDocId { get; set; }   // meta.identifier  → store as AffindaJobId

    // Flat fields → map into CandidateCv
    public string? ParsedName { get; set; }
    public string? ParsedPhone { get; set; }
    public string? ParsedEmail { get; set; }
    public string? ParsedTrade { get; set; }
    public int? ParsedExperienceYrs { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public List<string> ParsedSkills { get; set; } = new();

    // Profile hints (auto-fill if blank)
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    // Structured data → insert into child tables
    public List<AffindaWorkExp> WorkExperiences { get; set; } = new();
    public List<AffindaEducation> Educations { get; set; } = new();
    public List<AffindaLanguage> Languages { get; set; } = new();

    // Store raw JSON for future re-parsing
    public string? RawAffindaJson { get; set; }
}
