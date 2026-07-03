using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace JobPortal.Domain.Common
{

    public class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException($"Invalid value for enum {typeof(TEnum).Name}.");

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();

                if (enumMember?.Value?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return (TEnum)field.GetValue(null)!;
                }

                if (field.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return (TEnum)field.GetValue(null)!;
                }
            }

            throw new JsonException($"Unable to convert '{value}' to enum '{typeof(TEnum).Name}'.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            TEnum value,
            JsonSerializerOptions options)
        {
            var field = typeof(TEnum).GetField(value.ToString());

            var enumMember = field?
                .GetCustomAttribute<EnumMemberAttribute>();

            writer.WriteStringValue(
                enumMember?.Value ?? value.ToString());
        }
    }
}
