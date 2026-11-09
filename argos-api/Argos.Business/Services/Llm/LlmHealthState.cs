namespace Argos.Business.Services.Llm;

/// <summary>Último resultado conocido de un (proveedor, modelo). Mutable: lo protege un lock del tracker.</summary>
public sealed class LlmHealthState
{
    public const string Up = "up";
    public const string Down = "down";
    public const string Unknown = "unknown";

    public string Status { get; set; } = Unknown;
    public string? LastError { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public long? LatencyMs { get; set; }

    /// <summary>Fin del cooldown: hasta entonces el eslabón va al final de la cadena.</summary>
    public DateTimeOffset? RetryAfter { get; set; }
}
