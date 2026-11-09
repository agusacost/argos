using System.Net;

namespace Argos.Business.Services.Llm;

/// <summary>Resumen corto de un fallo de proveedor para avisos y para el estado de salud.</summary>
public static class LlmErrorDescriber
{
    private const int MaxLength = 160;

    public static string Describe(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: { } code } => $"HTTP {(int)code}{Hint(code)}",
        HttpRequestException e => $"error de red: {Shorten(e.Message)}",
        TaskCanceledException => "sin respuesta (timeout)",
        _ => Shorten(ex.Message),
    };

    private static string Hint(HttpStatusCode code) => (int)code switch
    {
        429 => " (límite de uso o cuota agotada)",
        401 or 403 => " (API key inválida o sin permisos)",
        404 => " (modelo inexistente)",
        >= 500 => " (servicio saturado o caído)",
        _ => "",
    };

    private static string Shorten(string message) => message.Length <= MaxLength ? message : message[..MaxLength] + "…";
}
