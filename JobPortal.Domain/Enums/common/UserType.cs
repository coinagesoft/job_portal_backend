using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JobPortal.Domain.Common;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<UserType>))]
    public enum UserType
    {
        [EnumMember(Value = "Candidate")]
        Candidate = 1,

        [EnumMember(Value = "Recruiter")]
        Recruiter = 2,

        [EnumMember(Value = "Admin")]
        Admin = 3,

        [EnumMember(Value = "Sub Admin")]
        SubAdmin = 4,

        [EnumMember(Value = "Both")]
        Both = 5
    }
}