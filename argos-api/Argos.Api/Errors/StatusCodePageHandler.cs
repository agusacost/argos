using Microsoft.AspNetCore.Diagnostics;

namespace Argos.Api.Errors;

/// <summary>Rutas inexistentes y métodos no permitidos con cuerpo <c>{"detail"}</c>.</summary>
public sealed class StatusCodePageHandler(IErrorResponseWriter writer) : IStatusCodePageHandler
{
    public Task HandleAsync(StatusCodeContext context)
    {
        var http = context.HttpContext;
        return http.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => writer.WriteAsync(http, StatusCodes.Status404NotFound, "Not Found"),
            StatusCodes.Status405MethodNotAllowed => writer.WriteAsync(http, StatusCodes.Status405MethodNotAllowed, "Method Not Allowed"),
            _ => Task.CompletedTask,
        };
    }
}
