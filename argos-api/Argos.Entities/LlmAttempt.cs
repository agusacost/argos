namespace Argos.Entities;

/// <summary>Un intento contra un eslabón de la cadena LLM.</summary>
public sealed record LlmAttempt(string Provider, string Model, bool Ok, string? Error, long LatencyMs);
