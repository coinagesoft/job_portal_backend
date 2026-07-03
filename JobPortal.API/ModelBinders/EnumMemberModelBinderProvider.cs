    using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace JobPortal.API.ModelBinders
{

    public class EnumMemberModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            var type = Nullable.GetUnderlyingType(context.Metadata.ModelType)
                       ?? context.Metadata.ModelType;

            if (type.IsEnum)
            {
                return new EnumMemberModelBinder();
            }

            return null;
        }
    }
}
