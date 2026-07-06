using JobPortal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Domain.Enums
{


    [JsonConverter(typeof(EnumMemberJsonConverter<SubUserRole>))]
    public enum SubUserRole
    {
        [EnumMember(Value = "Recruiter")]
        Recruiter,

        [EnumMember(Value = "HR Manager")]
        HR_Manager,

        [EnumMember(Value = "Viewer")]
        Viewer
    }
}
