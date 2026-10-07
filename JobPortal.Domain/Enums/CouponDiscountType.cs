using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JobPortal.Domain.Common;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<CouponDiscountType>))]
    public enum CouponDiscountType
    {
        [EnumMember(Value = "Percentage")]
        Percentage = 1,

        [EnumMember(Value = "Fixed")]
        Fixed = 2
    }
}