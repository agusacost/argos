using Microsoft.AspNetCore.Diagnostics;

namespace Argos.Api.Errors;

/// <summary>Cuerpo de las respuestas de error sin contenido (404 de ruta, 405 de método).</summary>
public interface IStatusCodePageHandler
{
    Task HandleAsync(StatusCodeContext context);
}
