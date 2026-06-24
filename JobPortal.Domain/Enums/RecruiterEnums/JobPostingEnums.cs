using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.RecruiterEnums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SalaryCurrency { INR, USD, AED, SAR ,EUR , GBP }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SalaryDisplayOption { Show_Range, Show_Min_Only, Negotiable, Show_Max_Only, }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GenderPreferred { Male, Female, Any }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationType { Onshore, Offshore }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompanyVisibility { ShowName, HideName }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobType { Normal_Job, Hot_Vacancy, Classified }


[JsonConverter(typeof(JsonStringEnumConverter))]

public enum EmploymentType
{
    Full_Time,
    Part_Time,
    Contract,
    Internship,
    Freelance
}
[JsonConverter(typeof(JsonStringEnumConverter))]

public enum EmploymentMode
{
    Onsite,
    Remote,
    Hybrid
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EducationLevel
{
    Any, Tenth, Twelfth, ITI, ITI_Diploma, Diploma, Graduate, Post_Graduate
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    PlanPurchase = 1,
    CreditAllocation = 2,
    ProfileUnlock = 3,
    CvDownload = 4
}