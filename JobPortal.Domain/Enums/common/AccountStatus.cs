using JobPortal.Domain.Common;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<AccountStatus>))]
    public enum AccountStatus
    {
        [EnumMember(Value = "Pending")]
        Pending,

        [EnumMember(Value = "Trial")]
        Trial,

        [EnumMember(Value = "Active")]
        Active,

        [EnumMember(Value = "Suspended")]
        Suspended,

        [EnumMember(Value = "Rejected")]
        Rejected
    }

}
