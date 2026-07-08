using JobPortal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Domain.Enums.RecruiterEnums
{
    [JsonConverter(typeof(EnumMemberJsonConverter<SupportTicketType>))]

    public enum SupportTicketType
    {
        ProfileAndResume = 1,
        JobApplication = 2,
        PaymentAndBilling = 3,
        AccountAccess = 4,
        TechnicalIssue = 5,
        Other = 6
    }
}
