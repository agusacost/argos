namespace Argos.Api.Errors;

/// <summary>Escribe errores con el formato <c>{"detail": "..."}</c>, el que lee el dashboard.</summary>
public interface IErrorResponseWriter
{
    Task WriteAsync(HttpContext context, int statusCode, string detail);
}
