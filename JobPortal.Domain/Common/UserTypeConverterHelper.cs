using System.Reflection;
using System.Runtime.Serialization;

namespace JobPortal.Domain.Common
{
    // EF Core's built-in EnumToStringConverter<UserType> round-trips using
    // the enum MEMBER NAME ("SubAdmin"), not the [EnumMember(Value=...)]
    // attribute ("Sub Admin") declared on UserType for JSON purposes. Some
    // existing rows in the users.user_type column were written as
    // "Sub Admin" (with a space) — which the strict converter can't parse
    // back into UserType and throws InvalidOperationException on every
    // query that touches them.
    //
    // This helper keeps writes exactly as before (enum name, e.g.
    // "SubAdmin") but makes reads tolerant: it first tries the strict
    // enum-name parse, then falls back to matching the [EnumMember] value,
    // then falls back to a whitespace-insensitive match. That way old rows
    // load correctly without needing a data migration, and any brand-new
    // row keeps being written the same way it always was.
    public static class UserTypeConverterHelper
    {
        public static string ToProviderString(Enums.common.UserType value) => value.ToString();

        public static Enums.common.UserType FromProviderString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            var trimmed = value.Trim();

            // 1) Strict match on the enum member name ("SubAdmin", "Admin", ...).
            if (Enum.TryParse<Enums.common.UserType>(trimmed, ignoreCase: true, out var byName))
                return byName;

            // 2) Match on the [EnumMember(Value = "...")] attribute
            //    ("Sub Admin", "Candidate", ...).
            foreach (var field in typeof(Enums.common.UserType).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
                if (enumMember != null &&
                    string.Equals(enumMember.Value, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return (Enums.common.UserType)field.GetValue(null)!;
                }
            }

            // 3) Last resort — ignore whitespace entirely
            //    ("Sub Admin" -> "SubAdmin").
            var noSpaces = trimmed.Replace(" ", string.Empty);
            if (Enum.TryParse<Enums.common.UserType>(noSpaces, ignoreCase: true, out var byNameNoSpaces))
                return byNameNoSpaces;

            throw new InvalidOperationException(
                $"Cannot convert stored users.user_type value '{value}' to UserType.");
        }
    }
}