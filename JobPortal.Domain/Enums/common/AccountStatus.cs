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
        Rejected,

        /// <summary>
        /// Permanently deleted by the account owner via Settings ▸ Delete
        /// Account. The row is kept (soft-delete) for audit/billing-history
        /// purposes, but the account can never authenticate again and is
        /// excluded from every candidate/admin-facing query.
        /// </summary>
        [EnumMember(Value = "Deleted")]
        Deleted
    }

}