using Argos.Common.Exceptions;

namespace Argos.Api.Errors;

/// <summary>
/// Contrato de errores de la API: errores esperados → su status con <c>{"detail"}</c>; cualquier
/// otro → 500 en texto plano ("Internal Server Error"), sin detalles internos.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    IExceptionStatusMapper mapper,
    IErrorResponseWriter writer,
    ILogger<ExceptionHandlingMiddleware> log) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var mapping = mapper.Map(ex);
            if (mapping is not null)
            {
                if (ex is DataStoreUnavailableException) log.LogError(ex, "Qdrant");
                await writer.WriteAsync(context, mapping.StatusCode, mapping.Detail);
                return;
            }
            log.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Internal Server Error");
        }
    }
}
