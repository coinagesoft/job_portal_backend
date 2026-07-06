using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JobPortal.Domain.Common;

namespace JobPortal.Domain.Enums.Common
{
    [JsonConverter(typeof(EnumMemberJsonConverter<CompanySize>))]
    public enum CompanySize
    {
        [EnumMember(Value = "1-10")]
        Size_1_10,

        [EnumMember(Value = "11-50")]
        Size_11_50,

        [EnumMember(Value = "51-200")]
        Size_51_200,

        [EnumMember(Value = "201-500")]
        Size_201_500,

        [EnumMember(Value = "500+")]
        Size_500_Plus
    }
}