using JobPortal.Domain.Common;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<PaymentStatus>))]
    public enum PaymentStatus
    {
        [EnumMember(Value = "Unpaid")]
        Unpaid = 1,

        [EnumMember(Value = "Paid")]
        Paid = 2,

        [EnumMember(Value = "Refunded")]
        Refunded = 3,

        [EnumMember(Value = "Failed")]
        Failed = 4
    }

}