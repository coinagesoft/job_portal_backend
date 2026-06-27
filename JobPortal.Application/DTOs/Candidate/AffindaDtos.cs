using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.AI;

//======================================================
// Root Response
//======================================================
public class AffindaDatePoint
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("day")]
    public int? Day { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; }
}
public class AffindaSingleDocumentResponse
{
    [JsonPropertyName("data")]
    public AffindaResumeData? Data { get; set; }

    [JsonPropertyName("meta")]
    public AffindaMeta? Meta { get; set; }

    [JsonPropertyName("error")]
    public AffindaError? Error { get; set; }
}

public class AffindaDocumentListResponse
{
    [JsonPropertyName("data")]
    public AffindaResumeData? Data { get; set; }

    [JsonPropertyName("meta")]
    public AffindaMeta? Meta { get; set; }

    [JsonPropertyName("error")]
    public AffindaError? Error { get; set; }
}

//======================================================
// Meta
//======================================================

public class AffindaMeta
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("failed")]
    public bool Failed { get; set; }

    [JsonPropertyName("readyDt")]
    public DateTime? ReadyDt { get; set; }

    [JsonPropertyName("ocrConfidence")]
    public decimal? OcrConfidence { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
}

public class AffindaError
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorDetail")]
    public string? ErrorDetail { get; set; }
}

//======================================================
// Resume Data
//======================================================

public class AffindaResumeData
{
    [JsonPropertyName("candidateName")]
    public AffindaCandidateName? CandidateName { get; set; }

    [JsonPropertyName("email")]
    public List<string>? Email { get; set; }

    [JsonPropertyName("phoneNumber")]
    public List<AffindaPhoneNumber>? PhoneNumber { get; set; }

    [JsonPropertyName("location")]
    public AffindaLocation? Location { get; set; }

    [JsonPropertyName("summary")]
    public AffindaTextField? Summary { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public AffindaTextField? DateOfBirth { get; set; }

    [JsonPropertyName("nationality")]
    public AffindaTextField? Nationality { get; set; }

    [JsonPropertyName("totalYearsExperience")]
    public AffindaDecimalField? TotalYearsExperience { get; set; }

    [JsonPropertyName("skill")]
    public List<AffindaSkill>? Skill { get; set; }

    [JsonPropertyName("education")]
    public List<AffindaEducation>? Education { get; set; }

    [JsonPropertyName("workExperience")]
    public List<AffindaWorkExp>? WorkExperience { get; set; }

    [JsonPropertyName("language")]
    public List<AffindaLanguage>? Language { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }
}

//======================================================
// Common Parsed Value
//======================================================

public class AffindaParsedValue
{
    [JsonPropertyName("parsed")]
    public string? Parsed { get; set; }
}

public class AffindaTextField
{
    [JsonPropertyName("parsed")]
    public string? Parsed { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }

    [JsonPropertyName("confidence")]
    public decimal? Confidence { get; set; }
}
public class AffindaDecimalField
{
    [JsonPropertyName("parsed")]
    public decimal? Parsed { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }
}
//
// Candidate Name
//

public class AffindaCandidateName
{
    [JsonPropertyName("parsed")]
    public AffindaCandidateNameParsed? Parsed { get; set; }
}

public class AffindaCandidateNameParsed
{
    [JsonPropertyName("firstName")]
    public AffindaParsedValue? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public AffindaParsedValue? MiddleName { get; set; }

    [JsonPropertyName("familyName")]
    public AffindaParsedValue? FamilyName { get; set; }

    [JsonPropertyName("title")]
    public AffindaParsedValue? Title { get; set; }
}

//
// Phone Number
//

public class AffindaPhoneNumber
{
    [JsonPropertyName("formattedNumber")]
    public string? FormattedNumber { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("internationalCountryCode")]
    public int? InternationalCountryCode { get; set; }
}

//
// Location
//

public class AffindaLocation
{
    [JsonPropertyName("parsed")]
    public AffindaLocationParsed? Parsed { get; set; }
}

public class AffindaLocationParsed
{
    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
}

//
// Skills
//

public class AffindaSkill
{
    [JsonPropertyName("parsed")]
    public AffindaSkillParsed? Parsed { get; set; }
}

public class AffindaSkillParsed
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("subCategory")]
    public string? SubCategory { get; set; }

    [JsonPropertyName("isSoftware")]
    public bool IsSoftware { get; set; }

    [JsonPropertyName("isLanguage")]
    public bool IsLanguage { get; set; }
}
//======================================================
// Work Experience
//======================================================

public class AffindaWorkExp
{
    [JsonPropertyName("parsed")]
    public AffindaWorkExpParsed? Parsed { get; set; }
}

public class AffindaWorkExpParsed
{
    [JsonPropertyName("workExperienceJobTitle")]
    public AffindaParsedValue? WorkExperienceJobTitle { get; set; }

    [JsonPropertyName("workExperienceOrganization")]
    public AffindaParsedValue? WorkExperienceOrganization { get; set; }

    [JsonPropertyName("workExperienceDescription")]
    public AffindaParsedValue? WorkExperienceDescription { get; set; }

    [JsonPropertyName("workExperienceLocation")]
    public AffindaLocationParsed? WorkExperienceLocation { get; set; }

    [JsonPropertyName("workExperienceDates")]
    public AffindaWorkDates? WorkExperienceDates { get; set; }
}

public class AffindaWorkDates
{
    [JsonPropertyName("start")]
    public AffindaDatePoint? Start { get; set; }

    [JsonPropertyName("end")]
    public AffindaDatePoint? End { get; set; }

    [JsonPropertyName("durationInMonths")]
    public int? DurationInMonths { get; set; }
}

//======================================================
// Education
//======================================================

public class AffindaEducation
{
    [JsonPropertyName("parsed")]
    public AffindaEducationParsed? Parsed { get; set; }
}

public class AffindaEducationParsed
{
    [JsonPropertyName("educationAccreditation")]
    public AffindaParsedValue? EducationAccreditation { get; set; }

    [JsonPropertyName("educationOrganization")]
    public AffindaParsedValue? EducationOrganization { get; set; }

    [JsonPropertyName("educationLevel")]
    public AffindaLabelValue? EducationLevel { get; set; }

    [JsonPropertyName("educationDates")]
    public AffindaEduDates? EducationDates { get; set; }

    [JsonPropertyName("educationGrade")]
    public AffindaEduGrade? EducationGrade { get; set; }
}

public class AffindaEduDates
{
    [JsonPropertyName("start")]
    public AffindaDatePoint? Start { get; set; }

    [JsonPropertyName("end")]
    public AffindaDatePoint? End { get; set; }
}

public class AffindaEduGrade
{
    [JsonPropertyName("educationGradeScore")]
    public decimal? EducationGradeScore { get; set; }

    [JsonPropertyName("gradeScore")]
    public decimal? GradeScore { get; set; }

    [JsonPropertyName("gradeUnit")]
    public AffindaLabelValue? GradeUnit { get; set; }
}

//======================================================
// Language
//======================================================

public class AffindaLanguage
{
    [JsonPropertyName("parsed")]
    public AffindaParsedValue? Parsed { get; set; }
}

//======================================================
// Label / Value
//======================================================

public class AffindaLabelValue
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
//======================================================
// Result returned by AffindaService
//======================================================

public class AffindaParseResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? AffindaDocId { get; set; }

    public string? ParsedName { get; set; }

    public string? ParsedPhone { get; set; }

    public string? ParsedEmail { get; set; }

    public string? ParsedTrade { get; set; }

    public int? ParsedExperienceYrs { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public List<string> ParsedSkills { get; set; } = new();
    public string? ProfessionalSummary { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public List<AffindaWorkExp> WorkExperiences { get; set; } = new();

    public List<AffindaEducation> Educations { get; set; } = new();

    public List<AffindaLanguage> Languages { get; set; } = new();

    public string? RawAffindaJson { get; set; }
}