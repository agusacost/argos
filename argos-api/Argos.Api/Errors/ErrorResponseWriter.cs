using System.Text.Json;
using Argos.Common.Serialization;
using Argos.Entities.Dtos;

namespace Argos.Api.Errors;

public sealed class ErrorResponseWriter : IErrorResponseWriter
{
    public Task WriteAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse(detail), JsonDefaults.Options));
    }
}
