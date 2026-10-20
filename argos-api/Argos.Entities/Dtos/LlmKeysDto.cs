namespace Argos.Entities.Dtos;

/// <summary>Qué API keys de LLM están configuradas en el servidor (solo lectura).</summary>
public sealed record LlmKeysDto(bool Gemini, bool Anthropic);
