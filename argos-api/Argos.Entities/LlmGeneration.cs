namespace Argos.Entities;

/// <summary>Texto generado, quién lo generó y los intentos fallidos previos de la cadena.</summary>
public sealed record LlmGeneration(string Text, string Provider, string Model, bool FallbackUsed, IReadOnlyList<LlmAttempt> Attempts)
{
    /// <summary>Direcciones IP distintas que se reemplazaron por marcadores antes de enviar el prompt al proveedor.</summary>
    public int PseudonymizedIps { get; init; }
}
