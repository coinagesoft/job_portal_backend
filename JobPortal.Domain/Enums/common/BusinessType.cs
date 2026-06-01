using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Domain.Enums.common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BusinessType
    {
        Proprietorship,
        Partnership,
        Private_Ltd,
        Public_Ltd,
        LLP,
        Other
    }
}
