using JobPortal.Domain.Common;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<KycStatus>))]
    public enum KycStatus
    {
        [EnumMember(Value = "Pending")]
        Pending = 1,

        [EnumMember(Value = "Approved")]
        Approved = 2,

        [EnumMember(Value = "Rejected")]
        Rejected = 3
    }

}