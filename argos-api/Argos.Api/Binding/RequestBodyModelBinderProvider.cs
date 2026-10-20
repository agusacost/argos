using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Argos.Api.Binding;

/// <summary>Usa <see cref="RequestBodyModelBinder"/> (resuelto por DI) para todo parámetro que sea un <see cref="IRequestDto"/>.</summary>
public sealed class RequestBodyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        typeof(IRequestDto).IsAssignableFrom(context.Metadata.ModelType)
            ? new BinderTypeModelBinder(typeof(RequestBodyModelBinder))
            : null;
}
