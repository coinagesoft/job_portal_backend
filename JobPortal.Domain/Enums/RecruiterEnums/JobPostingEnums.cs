using JobPortal.Domain.Common;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.RecruiterEnums;

[JsonConverter(typeof(EnumMemberJsonConverter<SalaryCurrency>))]
public enum SalaryCurrency
{
    [EnumMember(Value = "INR")]
    INR,

    [EnumMember(Value = "USD")]
    USD,

    [EnumMember(Value = "AED")]
    AED,

    [EnumMember(Value = "SAR")]
    SAR,

    [EnumMember(Value = "EUR")]
    EUR,

    [EnumMember(Value = "GBP")]
    GBP
}

[JsonConverter(typeof(EnumMemberJsonConverter<SalaryDisplayOption>))]
public enum SalaryDisplayOption
{
    [EnumMember(Value = "Show Range")]
    Show_Range,

    [EnumMember(Value = "Show Minimum Only")]
    Show_Min_Only,

    [EnumMember(Value = "Show Maximum Only")]
    Show_Max_Only,

    [EnumMember(Value = "Negotiable")]
    Negotiable
}

[JsonConverter(typeof(EnumMemberJsonConverter<GenderPreferred>))]
public enum GenderPreferred
{
    [EnumMember(Value = "Male")]
    Male,

    [EnumMember(Value = "Female")]
    Female,

    [EnumMember(Value = "Any")]
    Any
}

[JsonConverter(typeof(EnumMemberJsonConverter<LocationType>))]
public enum LocationType
{
    [EnumMember(Value = "Onshore")]
    Onshore,

    [EnumMember(Value = "Offshore")]
    Offshore
}

[JsonConverter(typeof(EnumMemberJsonConverter<CompanyVisibility>))]
public enum CompanyVisibility
{
    [EnumMember(Value = "Show Name")]
    ShowName,

    [EnumMember(Value = "Hide Name")]
    HideName
}

[JsonConverter(typeof(EnumMemberJsonConverter<JobType>))]
public enum JobType
{
    [EnumMember(Value = "Normal Job")]
    Normal_Job,

    [EnumMember(Value = "Hot Vacancy")]
    Hot_Vacancy,

    [EnumMember(Value = "Classified")]
    Classified
}

[JsonConverter(typeof(EnumMemberJsonConverter<EmploymentType>))]
public enum EmploymentType
{
    [EnumMember(Value = "Full Time")]
    Full_Time,

    [EnumMember(Value = "Part Time")]
    Part_Time,

    [EnumMember(Value = "Contract")]
    Contract,

    [EnumMember(Value = "Internship")]
    Internship,

    [EnumMember(Value = "Freelance")]
    Freelance
}

[JsonConverter(typeof(EnumMemberJsonConverter<EmploymentMode>))]
public enum EmploymentMode
{
    [EnumMember(Value = "Onsite")]
    Onsite,

    [EnumMember(Value = "Remote")]
    Remote,

    [EnumMember(Value = "Hybrid")]
    Hybrid
}

[JsonConverter(typeof(EnumMemberJsonConverter<EducationLevel>))]
public enum EducationLevel
{
    [EnumMember(Value = "Any")]
    Any,

    [EnumMember(Value = "10th")]
    Tenth,

    [EnumMember(Value = "12th")]
    Twelfth,

    [EnumMember(Value = "ITI")]
    ITI,

    [EnumMember(Value = "ITI Diploma")]
    ITI_Diploma,

    [EnumMember(Value = "Diploma")]
    Diploma,

    [EnumMember(Value = "Graduate")]
    Graduate,

    [EnumMember(Value = "Post Graduate")]
    Post_Graduate
}

[JsonConverter(typeof(EnumMemberJsonConverter<TransactionType>))]
public enum TransactionType
{
    [EnumMember(Value = "Plan Purchase")]
    PlanPurchase = 1,

    [EnumMember(Value = "Credit Allocation")]
    CreditAllocation = 2,

    [EnumMember(Value = "Profile Unlock")]
    ProfileUnlock = 3,

    [EnumMember(Value = "CV Download")]
    CvDownload = 4
}