namespace Argos.Common.Configuration;

/// <summary>Proveedores LLM (las API keys viven solo en el entorno).</summary>
public interface ILlmConfig
{
    string LlmProvider { get; }
    string GeminiApiKey { get; }
    string GeminiModel { get; }
    string AnthropicApiKey { get; }
    string ClaudeModel { get; }
    string GroqApiKey { get; }
    string GroqModel { get; }
    string OpenRouterApiKey { get; }
    string OpenRouterModel { get; }

    /// <summary>Respaldo en orden, después del proveedor principal: <c>proveedor[:modelo]</c> separados por coma.</summary>
    string LlmFallbackChain { get; }

    /// <summary>Tiempo que un proveedor que falló queda fuera de la cadena antes de reintentarlo.</summary>
    int LlmCooldownSeconds { get; }
}
