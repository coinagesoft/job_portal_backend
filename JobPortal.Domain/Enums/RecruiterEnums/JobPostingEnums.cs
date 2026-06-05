using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.JobPosting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SalaryCurrency { INR, USD, AED, SAR }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SalaryDisplayOption { Show_Range, Show_Min_Only, Confidential }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GenderPreferred { Male, Female, Any }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationType { Onshore, Offshore }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompanyVisibility { Show_Name, Confidential_Client }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobType { Normal_Job, Hot_Vacancy, Classified }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmploymentType { Permanent, Contract, Temporary, Internship }

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