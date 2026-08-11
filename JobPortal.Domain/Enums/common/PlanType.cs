using JobPortal.Domain.Common;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<PlanType>))]
    public enum PlanType
    {
        [EnumMember(Value = "Recruiter")]
        Recruiter = 1,

        [EnumMember(Value = "Candidate")]
        Candidate = 2
    }
}