namespace Argos.Common.Configuration;

/// <summary>Proveedores LLM (las API keys viven solo en el entorno).</summary>
public interface ILlmConfig
{
    string LlmProvider { get; }
    string GeminiApiKey { get; }
    string GeminiModel { get; }
    string AnthropicApiKey { get; }
    string ClaudeModel { get; }
}
