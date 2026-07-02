using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobPortal.Domain.Common
{
    /// <summary>
    /// Generic JSON converter that honors [EnumMember(Value = "...")] attributes,
    /// unlike the built-in JsonStringEnumConverter which ignores them.
    /// Falls back to the raw enum name if no [EnumMember] is present.
    /// </summary>
    public class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        private static readonly Dictionary<TEnum, string> ToJson = new();
        private static readonly Dictionary<string, TEnum> FromJson = new(StringComparer.OrdinalIgnoreCase);

        static EnumMemberJsonConverter()
        {
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumValue = (TEnum)field.GetValue(null)!;
                var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                var jsonValue = attr?.Value ?? field.Name;

                ToJson[enumValue] = jsonValue;
                FromJson[jsonValue] = enumValue;
            }
        }

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var raw = reader.GetString();

            if (raw != null && FromJson.TryGetValue(raw, out var value))
                return value;

            if (raw != null && Enum.TryParse<TEnum>(raw, true, out var parsed))
                return parsed;

            throw new JsonException($"Unable to convert \"{raw}\" to enum {typeof(TEnum)}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ToJson.TryGetValue(value, out var jsonValue) ? jsonValue : value.ToString());
        }
    }
}