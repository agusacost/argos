using Argos.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Argos.Api.Filters;

/// <summary>Parámetros de query/ruta con tipo inválido (p.ej. <c>?page=abc</c>) → 422.</summary>
public sealed class ModelStateValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;
        var (key, entry) = context.ModelState.First(e => e.Value is { Errors.Count: > 0 });
        throw new InvalidInputException($"Parámetro '{key}': {entry!.Errors[0].ErrorMessage}");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
