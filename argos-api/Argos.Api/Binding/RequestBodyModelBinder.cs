using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Argos.Api.Binding;

/// <summary>Binder de los DTOs de entrada (<see cref="Argos.Entities.Dtos.IRequestDto"/>).</summary>
public sealed class RequestBodyModelBinder(IRequestBodyReader reader) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var model = await reader.ReadAsync(bindingContext.HttpContext.Request, bindingContext.ModelType);
        bindingContext.Result = ModelBindingResult.Success(model);
    }
}
