using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace JobPortal.API.ModelBinders
{


    public class EnumMemberModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var value = bindingContext.ValueProvider
                .GetValue(bindingContext.ModelName)
                .FirstValue;

            if (string.IsNullOrWhiteSpace(value))
                return Task.CompletedTask;

            var enumType = Nullable.GetUnderlyingType(bindingContext.ModelType)
                           ?? bindingContext.ModelType;

            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // Match EnumMember value
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();

                if (enumMember?.Value?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
                {
                    bindingContext.Result = ModelBindingResult.Success(
                        Enum.Parse(enumType, field.Name));

                    return Task.CompletedTask;
                }

                // Match enum name
                if (field.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    bindingContext.Result = ModelBindingResult.Success(
                        Enum.Parse(enumType, field.Name));

                    return Task.CompletedTask;
                }
            }

            // Match the underlying numeric value (e.g. "0", "1") so Swagger's
            // generated dropdown — which posts the raw enum value, not the
            // member name — still binds correctly. Without this, every
            // numeric selection from Swagger UI fails model binding with a
            // "not valid for <EnumType>" 400, even for values that are
            // perfectly valid (e.g. ActorType=0 for Admin).
            if (int.TryParse(value, out var numericValue) && Enum.IsDefined(enumType, numericValue))
            {
                bindingContext.Result = ModelBindingResult.Success(
                    Enum.ToObject(enumType, numericValue));

                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"The value '{value}' is not valid for {enumType.Name}.");

            return Task.CompletedTask;
        }
    }
}