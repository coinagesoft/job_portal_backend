
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

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"The value '{value}' is not valid for {enumType.Name}.");

            return Task.CompletedTask;
        }
    }
}
