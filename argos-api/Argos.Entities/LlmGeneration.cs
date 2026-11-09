namespace Argos.Entities;

/// <summary>Texto generado, quién lo generó y los intentos fallidos previos de la cadena.</summary>
public sealed record LlmGeneration(string Text, string Provider, string Model, bool FallbackUsed, IReadOnlyList<LlmAttempt> Attempts);
