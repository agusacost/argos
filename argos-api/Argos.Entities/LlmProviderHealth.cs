namespace Argos.Entities;

/// <summary>
/// Estado de un eslabón de la cadena LLM según las últimas llamadas reales (o un chequeo manual).
/// <c>Status</c>: up | down | unknown | unconfigured. <c>Available</c> es false mientras dura el cooldown.
/// </summary>
public sealed record LlmProviderHealth(
    string Provider,
    string Model,
    string Role,
    bool Configured,
    string Status,
    bool Available,
    string? LastError,
    string? LastCheckedAt,
    string? LastSuccessAt,
    long? LatencyMs,
    string? RetryAfter);
